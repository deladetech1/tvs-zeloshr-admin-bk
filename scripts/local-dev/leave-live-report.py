#!/usr/bin/env python3
"""Run all Leave API endpoints against live dev and write a self-contained HTML report."""

from __future__ import annotations

import html
import json
import os
import sys
import textwrap
from dataclasses import dataclass, field
from datetime import date, datetime, timedelta, timezone
from pathlib import Path
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[2]
REPORT_ROOT = ROOT / "reports" / "e2e" / "leave" / "live-report"
REPORT_LATEST = REPORT_ROOT / "latest"


@dataclass
class StepResult:
    name: str
    method: str
    path: str
    request_headers: dict[str, str]
    request_body: Any | None
    status: int | None = None
    response_headers: dict[str, str] = field(default_factory=dict)
    response_body: Any | None = None
    error: str | None = None
    note: str | None = None
    expected_status: int | None = None

    @property
    def ok(self) -> bool:
        if self.error:
            return False
        if self.status is None:
            return False
        if self.expected_status is not None:
            return self.status == self.expected_status
        if self.note and self.note.startswith("expected:"):
            return True
        return 200 <= self.status < 300


class LeaveLiveReporter:
    def __init__(self) -> None:
        self.base = os.environ.get(
            "ZELOSHR_API_BASE", "https://zeloshr.app.backend.dev.trovesuite.com"
        ).rstrip("/")
        self.token = os.environ.get("TROVE_BEARER_TOKEN", "")
        self.app_id = os.environ.get("TROVE_APP_ID", "app-hr")
        self.org_id = os.environ.get("TROVE_ORG_ID", "")
        self.bus_id = os.environ.get("TROVE_BUS_ID", "")
        self.loc_id = os.environ.get("TROVE_LOC_ID", "")
        self.run_tag = datetime.now(timezone.utc).strftime("live-%Y%m%d-%H%M%S")
        self.steps: list[StepResult] = []
        self.ctx: dict[str, str] = {}

        if not self.token:
            sys.exit("TROVE_BEARER_TOKEN is empty — set in scripts/local-dev/live-session.env")

    def headers(self, with_json: bool = False) -> dict[str, str]:
        h = {
            "accept": "application/json",
            "app-id": self.app_id,
            "authorization": f"Bearer {self.token}",
            "bus-id": self.bus_id,
            "loc-id": self.loc_id,
            "org-id": self.org_id,
        }
        if with_json:
            h["content-type"] = "application/json"
        return h

    def call(
        self,
        name: str,
        method: str,
        path: str,
        body: Any | None = None,
        *,
        note: str | None = None,
        expected_status: int | None = None,
    ) -> StepResult:
        url = f"{self.base}{path}"
        data = None
        if body is not None:
            data = json.dumps(body).encode("utf-8")

        req_headers = self.headers(with_json=body is not None)
        req = Request(url, data=data, headers=req_headers, method=method)

        step = StepResult(
            name=name,
            method=method,
            path=path,
            request_headers=req_headers,
            request_body=body,
            note=note,
            expected_status=expected_status,
        )

        try:
            with urlopen(req, timeout=60) as resp:
                step.status = resp.status
                step.response_headers = dict(resp.headers.items())
                raw = resp.read().decode("utf-8", errors="replace")
                try:
                    step.response_body = json.loads(raw) if raw else None
                except json.JSONDecodeError:
                    step.response_body = raw
        except HTTPError as exc:
            step.status = exc.code
            step.response_headers = dict(exc.headers.items())
            raw = exc.read().decode("utf-8", errors="replace")
            try:
                step.response_body = json.loads(raw) if raw else None
            except json.JSONDecodeError:
                step.response_body = raw
        except URLError as exc:
            step.error = str(exc.reason)

        if (
            expected_status is not None
            and step.status != expected_status
            and not step.error
        ):
            step.note = (
                (step.note + " — ") if step.note else ""
            ) + f"expected HTTP {expected_status}, got {step.status}"

        self.steps.append(step)
        return step

    def jpath(self, step: StepResult, *keys: str) -> Any:
        data = step.response_body
        if not isinstance(data, dict):
            return None
        cur: Any = data
        for key in keys:
            if not isinstance(cur, dict):
                return None
            cur = cur.get(key)
        return cur

    def capture(self, step: StepResult, key: str, *path: str) -> None:
        val = self.jpath(step, *path)
        if val is not None:
            self.ctx[key] = str(val)

    def run(self) -> int:
        today = date.today()
        start = (today + timedelta(days=60)).isoformat()
        end = (today + timedelta(days=64)).isoformat()
        delete_start = (today + timedelta(days=90)).isoformat()
        holiday_date = (today + timedelta(days=120)).isoformat()

        # --- Reads (admin) ---
        self.call("Statistics", "GET", "/api/v1/leave/statistics")
        self.call("Dashboard", "GET", "/api/v1/leave/dashboard")
        self.call("Requests list (default)", "GET", "/api/v1/leave/requests/list?page=1&size=5")
        self.call(
            "Requests list (pending final)",
            "GET",
            "/api/v1/leave/requests/list?approval_stage=pending_final&status=Pending&page=1&size=5",
        )
        self.call(
            "Requests list (approved)",
            "GET",
            "/api/v1/leave/requests/list?status=Approved&page=1&size=5",
        )
        self.call(
            "Requests list (rejected)",
            "GET",
            "/api/v1/leave/requests/list?status=Rejected&page=1&size=5",
        )
        self.call(
            "Requests list (search)",
            "GET",
            "/api/v1/leave/requests/list?search=ama&page=1&size=5",
        )
        self.call(
            "Requests list (date range)",
            "GET",
            f"/api/v1/leave/requests/list?from_date={start}&to_date={end}&page=1&size=5",
        )

        self.call("Balances list", "GET", "/api/v1/leave/balances/list")
        self.call("Types list", "GET", "/api/v1/leave/types/list?active_only=true")
        self.call(
            "Holidays list",
            "GET",
            "/api/v1/leave/holidays/list?country_code=GH&year=2026&page=1&size=10",
        )

        # --- Leave type CRUD ---
        type_add = self.call(
            "Create leave type",
            "POST",
            "/api/v1/leave/types/add",
            {
                "name": f"Live report {self.run_tag}",
                "default_entitled_days": 21,
                "is_paid": True,
                "is_active": True,
            },
        )
        self.capture(type_add, "leave_type_id", "data", "leave_type_id")

        if self.ctx.get("leave_type_id"):
            lt = self.ctx["leave_type_id"]
            self.call(
                "Get leave type",
                "GET",
                f"/api/v1/leave/types/get?leave_type_id={lt}",
            )
            self.call(
                "Update leave type",
                "PUT",
                f"/api/v1/leave/types/update?leave_type_id={lt}",
                {"name": f"Live report updated {self.run_tag}", "default_entitled_days": 22},
            )
            self.call(
                "Requests list (filter by leave_type_id)",
                "GET",
                f"/api/v1/leave/requests/list?leave_type_id={lt}&page=1&size=5",
            )
            self.call(
                "Balances list (filter by leave_type_id)",
                "GET",
                f"/api/v1/leave/balances/list?leave_type_id={lt}",
            )

        # --- Employee for mutations ---
        emp_list = self.call(
            "Employees list (pick first for mutations)",
            "GET",
            "/api/v1/employees/list?page=1&size=1",
        )
        self.capture(emp_list, "employee_id", "data", "items")
        if isinstance(emp_list.response_body, dict):
            items = self.jpath(emp_list, "data", "items")
            if isinstance(items, list) and items:
                eid = items[0].get("employee_id")
                if eid:
                    self.ctx["employee_id"] = str(eid)

        employee_id = self.ctx.get("employee_id")
        leave_type_id = self.ctx.get("leave_type_id")

        self.call(
            "Leave summary (admin dashboard)",
            "GET",
            "/api/v1/leave/summary",
        )

        if employee_id:
            self.call(
                "My summary (personal)",
                "GET",
                f"/api/v1/leave/my/summary?employee_id={employee_id}",
            )
            self.call(
                "My balances list",
                "GET",
                f"/api/v1/leave/my/balances/list?employee_id={employee_id}",
            )
            self.call(
                "My requests list",
                "GET",
                f"/api/v1/leave/my/requests/list?employee_id={employee_id}&page=1&size=5",
            )

        if employee_id:
            self.call(
                "Requests list (filter by employee_id)",
                "GET",
                f"/api/v1/leave/requests/list?employee_id={employee_id}&page=1&size=5",
            )
            self.call(
                "Balances list (filter by employee_id)",
                "GET",
                f"/api/v1/leave/balances/list?employee_id={employee_id}",
            )

        # --- Balance CRUD ---
        if employee_id and leave_type_id:
            bal_add = self.call(
                "Create leave balance",
                "POST",
                "/api/v1/leave/balances/add",
                {
                    "employee_id": employee_id,
                    "leave_type_id": leave_type_id,
                    "entitled_days": 21,
                    "used_days": 0,
                },
            )
            if isinstance(bal_add.response_body, dict):
                lid = self.jpath(bal_add, "data", "leave_balance_id")
                if lid:
                    self.ctx["leave_balance_id"] = str(lid)
            elif bal_add.status == 400:
                # may already exist — pick from list
                bal_list = self.call(
                    "Balances list (reuse existing row)",
                    "GET",
                    f"/api/v1/leave/balances/list?employee_id={employee_id}&leave_type_id={leave_type_id}",
                )
                items = self.jpath(bal_list, "data", "items")
                if isinstance(items, list) and items:
                    self.ctx["leave_balance_id"] = str(items[0].get("leave_balance_id", ""))

        balance_id = self.ctx.get("leave_balance_id")
        if balance_id:
            self.call(
                "Get leave balance",
                "GET",
                f"/api/v1/leave/balances/get?leave_balance_id={balance_id}",
            )
            self.call(
                "Update leave balance",
                "PUT",
                f"/api/v1/leave/balances/update?leave_balance_id={balance_id}",
                {"entitled_days": 22, "used_days": 0},
            )

        # --- Request CRUD + reject ---
        if employee_id and leave_type_id:
            req_add = self.call(
                "Create leave request (admin)",
                "POST",
                "/api/v1/leave/requests/add",
                {
                    "employee_id": employee_id,
                    "leave_type_id": leave_type_id,
                    "start_date": start,
                    "end_date": end,
                    "days_requested": 5,
                    "notes": f"Live report {self.run_tag}",
                },
            )
            if isinstance(req_add.response_body, dict):
                rid = self.jpath(req_add, "data", "leave_request_id")
                if rid:
                    self.ctx["leave_request_id"] = str(rid)

        request_id = self.ctx.get("leave_request_id")
        if request_id:
            self.call(
                "Get leave request (detail modal)",
                "GET",
                f"/api/v1/leave/requests/get?leave_request_id={request_id}",
            )
            self.call(
                "Update leave request notes",
                "PUT",
                f"/api/v1/leave/requests/update?leave_request_id={request_id}",
                {"notes": f"Updated by live report {self.run_tag}"},
            )
            self.call(
                "Reject leave request",
                "POST",
                f"/api/v1/leave/requests/reject?leave_request_id={request_id}",
                {"notes": f"Rejected by live report {self.run_tag}"},
            )
            self.call(
                "Get leave request (after reject)",
                "GET",
                f"/api/v1/leave/requests/get?leave_request_id={request_id}",
            )

        # Delete flow (separate pending request)
        if employee_id and leave_type_id:
            del_add = self.call(
                "Create leave request (for delete test)",
                "POST",
                "/api/v1/leave/requests/add",
                {
                    "employee_id": employee_id,
                    "leave_type_id": leave_type_id,
                    "start_date": delete_start,
                    "end_date": delete_start,
                    "days_requested": 1,
                    "notes": f"Delete test {self.run_tag}",
                },
            )
            del_id = self.jpath(del_add, "data", "leave_request_id")
            if del_id:
                self.call(
                    "Delete leave request",
                    "DELETE",
                    f"/api/v1/leave/requests/delete?leave_request_id={del_id}",
                )
                self.call(
                    "Get deleted request (expect 404)",
                    "GET",
                    f"/api/v1/leave/requests/get?leave_request_id={del_id}",
                    expected_status=404,
                )

        # My Leave submit
        if leave_type_id and employee_id:
            self.call(
                "Create my leave request",
                "POST",
                f"/api/v1/leave/my/requests/add?employee_id={employee_id}",
                {
                    "leave_type_id": leave_type_id,
                    "start_date": start,
                    "end_date": end,
                    "days_requested": 5,
                    "notes": f"My leave {self.run_tag}",
                },
            )

        # Approve — document attempt on non-pending (409) or missing approver chain
        if request_id:
            self.call(
                "Approve rejected request (expect 409)",
                "POST",
                f"/api/v1/leave/requests/approve?leave_request_id={request_id}",
                note="expected:409 — request already rejected",
                expected_status=409,
            )

        # --- Holiday CRUD ---
        hol_add = self.call(
            "Create public holiday",
            "POST",
            "/api/v1/leave/holidays/add",
            {
                "country_code": "GH",
                "name": f"Live report holiday {self.run_tag}",
                "holiday_date": holiday_date,
                "is_recurring": False,
                "is_active": True,
            },
        )
        hol_id = self.jpath(hol_add, "data", "holiday_id")
        if hol_id:
            self.ctx["holiday_id"] = str(hol_id)
            self.call(
                "Get public holiday",
                "GET",
                f"/api/v1/leave/holidays/get?holiday_id={hol_id}",
            )
            self.call(
                "Update public holiday",
                "PUT",
                f"/api/v1/leave/holidays/update?holiday_id={hol_id}",
                {"name": f"Live report holiday updated {self.run_tag}"},
            )
            self.call(
                "Delete public holiday",
                "DELETE",
                f"/api/v1/leave/holidays/delete?holiday_id={hol_id}",
            )

        # Cleanup leave type if unused
        if leave_type_id:
            self.call(
                "Delete leave type",
                "DELETE",
                f"/api/v1/leave/types/delete?leave_type_id={leave_type_id}",
                note="may 409 if still referenced by balances/requests",
            )

        passed = sum(1 for s in self.steps if s.ok)
        failed = len(self.steps) - passed
        return 0 if failed == 0 else 1

    def write_html(self, run_dir: Path) -> Path:
        run_dir.mkdir(parents=True, exist_ok=True)
        out = run_dir / "index.html"

        passed = sum(1 for s in self.steps if s.ok)
        failed = len(self.steps) - passed

        def esc(s: Any) -> str:
            return html.escape("" if s is None else str(s))

        def fmt_json(obj: Any) -> str:
            if obj is None:
                return "(empty)"
            if isinstance(obj, str):
                return obj
            return json.dumps(obj, indent=2, ensure_ascii=False)

        def redact_headers(h: dict[str, str]) -> dict[str, str]:
            out_h = dict(h)
            if "authorization" in out_h:
                tok = out_h["authorization"]
                if len(tok) > 24:
                    out_h["authorization"] = tok[:16] + "…[REDACTED]"
            return out_h

        rows = []
        for i, step in enumerate(self.steps, 1):
            status_cls = "ok" if step.ok else "fail"
            status_label = f"HTTP {step.status}" if step.status else "ERROR"
            note = f'<p class="note">{esc(step.note)}</p>' if step.note else ""
            err = f'<p class="error">{esc(step.error)}</p>' if step.error else ""
            rows.append(
                f"""
                <section class="step {status_cls}" id="step-{i}">
                  <header>
                    <span class="num">{i}</span>
                    <h2>{esc(step.name)}</h2>
                    <span class="badge {status_cls}">{esc(status_label)}</span>
                    <code class="verb">{esc(step.method)}</code>
                    <code class="path">{esc(step.path)}</code>
                  </header>
                  {note}{err}
                  <div class="panels">
                    <details open>
                      <summary>Request</summary>
                      <h3>Headers</h3>
                      <pre>{esc(fmt_json(redact_headers(step.request_headers)))}</pre>
                      <h3>Body</h3>
                      <pre>{esc(fmt_json(step.request_body))}</pre>
                    </details>
                    <details open>
                      <summary>Response</summary>
                      <h3>Status</h3>
                      <pre>{esc(step.status)}</pre>
                      <h3>Body</h3>
                      <pre>{esc(fmt_json(step.response_body))}</pre>
                    </details>
                  </div>
                </section>
                """
            )

        doc = f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8"/>
  <title>Leave API live report — {esc(self.run_tag)}</title>
  <style>
    :root {{ font-family: ui-sans-serif, system-ui, sans-serif; color: #0f172a; background: #f8fafc; }}
    body {{ max-width: 1100px; margin: 0 auto; padding: 24px; }}
    h1 {{ margin-bottom: 4px; }}
    .meta {{ color: #475569; margin-bottom: 24px; }}
    .summary {{ display: flex; gap: 16px; margin-bottom: 32px; }}
    .summary div {{ background: #fff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px 16px; }}
    .step {{ background: #fff; border: 1px solid #e2e8f0; border-radius: 10px; margin-bottom: 20px; overflow: hidden; }}
    .step.ok {{ border-left: 4px solid #16a34a; }}
    .step.fail {{ border-left: 4px solid #dc2626; }}
    .step header {{ display: flex; flex-wrap: wrap; align-items: center; gap: 8px; padding: 14px 16px; background: #f1f5f9; }}
    .step header h2 {{ margin: 0; flex: 1 1 200px; font-size: 1.05rem; }}
    .num {{ background: #334155; color: #fff; border-radius: 999px; width: 28px; height: 28px; display: inline-flex; align-items: center; justify-content: center; font-size: 0.85rem; }}
    .badge {{ padding: 2px 8px; border-radius: 6px; font-size: 0.8rem; font-weight: 600; }}
    .badge.ok {{ background: #dcfce7; color: #166534; }}
    .badge.fail {{ background: #fee2e2; color: #991b1b; }}
    .verb {{ background: #dbeafe; color: #1e40af; padding: 2px 6px; border-radius: 4px; }}
    .path {{ color: #334155; word-break: break-all; }}
    .panels {{ padding: 0 16px 16px; display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }}
    @media (max-width: 900px) {{ .panels {{ grid-template-columns: 1fr; }} }}
    details {{ border: 1px solid #e2e8f0; border-radius: 8px; padding: 8px 12px; }}
    summary {{ cursor: pointer; font-weight: 600; }}
    pre {{ background: #0f172a; color: #e2e8f0; padding: 12px; border-radius: 6px; overflow: auto; font-size: 0.78rem; line-height: 1.45; max-height: 420px; }}
    .note {{ margin: 0 16px; color: #854d0e; background: #fef9c3; padding: 8px 12px; border-radius: 6px; }}
    .error {{ margin: 0 16px; color: #991b1b; background: #fee2e2; padding: 8px 12px; border-radius: 6px; }}
    nav {{ margin-bottom: 24px; }}
    nav a {{ color: #2563eb; text-decoration: none; margin-right: 12px; font-size: 0.9rem; }}
  </style>
</head>
<body>
  <h1>Leave API — live endpoint report</h1>
  <p class="meta">
    Run <strong>{esc(self.run_tag)}</strong> · Base <code>{esc(self.base)}</code> ·
    Org <code>{esc(self.org_id)}</code> · Generated {esc(datetime.now(timezone.utc).isoformat())}
  </p>
  <div class="summary">
    <div><strong>{len(self.steps)}</strong> steps</div>
    <div style="color:#166534"><strong>{passed}</strong> passed</div>
    <div style="color:#991b1b"><strong>{failed}</strong> failed / unexpected</div>
  </div>
  <nav>
    <strong>Jump:</strong>
    {''.join(f'<a href="#step-{i}">{esc(s.name)}</a>' for i, s in enumerate(self.steps, 1))}
  </nav>
  {''.join(rows)}
</body>
</html>"""

        out.write_text(doc, encoding="utf-8")
        return out


def main() -> int:
    run_id = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    run_dir = REPORT_ROOT / run_id
    reporter = LeaveLiveReporter()
    exit_code = reporter.run()
    html_path = reporter.write_html(run_dir)

    REPORT_LATEST.mkdir(parents=True, exist_ok=True)
    latest_html = REPORT_LATEST / "index.html"
    latest_html.write_text(html_path.read_text(encoding="utf-8"), encoding="utf-8")
    (REPORT_LATEST / "summary.txt").write_text(
        f"run_id={run_id}\nbase={reporter.base}\nexit_code={exit_code}\nhtml={latest_html}\n",
        encoding="utf-8",
    )

    print(f"Leave live report: {len(reporter.steps)} steps, exit {exit_code}")
    print(f"  file://{latest_html}")
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
