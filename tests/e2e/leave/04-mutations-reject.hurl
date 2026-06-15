# Leave — create, detail modal fields, reject (single admin token)

POST {{base}}/api/v1/leave/types/add
accept: application/json
content-type: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}
{
  "name": "E2E Reject {{run_tag}}",
  "default_entitled_days": 21,
  "is_paid": true,
  "accrual_method": "front_loaded",
  "carry_over_allowed": false,
  "applies_to_employment_types": ["Full-time", "Part-time", "Contract"],
  "requires_supporting_document": false
}

HTTP 200
[Captures]
leave_type_id: jsonpath "$.data.leave_type_id"
[Asserts]
jsonpath "$.success" == true

GET {{base}}/api/v1/employees/list?page=1&size=1
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Captures]
employee_id: jsonpath "$.data.items[0].employee_id"
[Asserts]
jsonpath "$.data.items" count >= 1

POST {{base}}/api/v1/leave/requests/add
accept: application/json
content-type: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}
{
  "employee_id": "{{employee_id}}",
  "leave_type_id": "{{leave_type_id}}",
  "start_date": "{{start_date}}",
  "end_date": "{{end_date}}",
  "days_requested": {{days_requested}},
  "notes": "E2E reject flow {{run_tag}}"
}

HTTP 200
[Captures]
leave_request_id: jsonpath "$.data.leave_request_id"
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.status" == "Pending"
jsonpath "$.data.employee.full_name" != null

GET {{base}}/api/v1/leave/requests/get?leave_request_id={{leave_request_id}}
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.working_days" isNumber
jsonpath "$.data.approval_trail" isCollection
jsonpath "$.data.approval_trail" count == 3
jsonpath "$.data.balance_impact" isObject
jsonpath "$.data.employee" isObject

POST {{base}}/api/v1/leave/requests/reject?leave_request_id={{leave_request_id}}
accept: application/json
content-type: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}
{
  "notes": "E2E rejected {{run_tag}}"
}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.status" == "Rejected"
jsonpath "$.data.approval_stage" == "rejected"

GET {{base}}/api/v1/leave/requests/get?leave_request_id={{leave_request_id}}
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.data.status" == "Rejected"
