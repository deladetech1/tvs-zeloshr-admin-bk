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

# Leave Management landing page (admin dashboard).
GET {{base}}/api/v1/leave/summary
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

# My Leave — personal balances only (requires employee_id).
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

GET {{base}}/api/v1/leave/my/summary?employee_id={{employee_id}}
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.balances" isCollection

GET {{base}}/api/v1/leave/my/balances/list?employee_id={{employee_id}}
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

GET {{base}}/api/v1/leave/my/requests/list?employee_id={{employee_id}}&page=1&size=5
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
