# Leave — read-only endpoints (admin + My Leave)

GET {{base}}/api/v1/leave/statistics
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.pending_requests" isInteger
jsonpath "$.data.pending_final_approvals" isInteger

GET {{base}}/api/v1/leave/dashboard
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.summary" isObject
jsonpath "$.data.on_leave_today" isCollection
jsonpath "$.data.pending_approvals" isCollection
jsonpath "$.data.leaving_this_week" isCollection

# My Leave requires a platform user linked to hr_employees (404 when absent).
GET {{base}}/api/v1/leave/my/summary
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 404
[Asserts]
jsonpath "$.success" == false
jsonpath "$.status_code" == 404

GET {{base}}/api/v1/leave/my/balances/list
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 404
[Asserts]
jsonpath "$.success" == false

GET {{base}}/api/v1/leave/my/requests/list?page=1&size=5
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 404
[Asserts]
jsonpath "$.success" == false

GET {{base}}/api/v1/leave/balances/list
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.items" isCollection
