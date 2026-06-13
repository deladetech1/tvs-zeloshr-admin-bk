# Leave — create pending request and delete (cleanup)

POST {{base}}/api/v1/leave/types/add
accept: application/json
content-type: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}
{
  "name": "E2E Delete {{run_tag}}",
  "default_entitled_days": 21,
  "is_paid": true,
  "is_active": true
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
  "start_date": "{{delete_start_date}}",
  "end_date": "{{delete_end_date}}",
  "days_requested": 1,
  "notes": "E2E delete flow {{run_tag}}"
}

HTTP 200
[Captures]
delete_request_id: jsonpath "$.data.leave_request_id"
[Asserts]
jsonpath "$.data.status" == "Pending"

DELETE {{base}}/api/v1/leave/requests/delete?leave_request_id={{delete_request_id}}
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true

GET {{base}}/api/v1/leave/requests/get?leave_request_id={{delete_request_id}}
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 404
