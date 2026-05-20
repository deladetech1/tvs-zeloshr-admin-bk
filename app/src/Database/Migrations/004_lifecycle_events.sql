CREATE TABLE IF NOT EXISTS zeloshr.zhr_lifecycle_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    event_type VARCHAR(80) NOT NULL,
    department_name VARCHAR(150),
    branch_name VARCHAR(150),
    due_date DATE NOT NULL,
    status VARCHAR(50) NOT NULL,
    urgency VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_zhr_lifecycle_events_due
    ON zeloshr.zhr_lifecycle_events (tenant_id, org_id, due_date);
