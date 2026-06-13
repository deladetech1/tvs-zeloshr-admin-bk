# Leave — admin list filters (Approvals UI variants)

GET {{base}}/api/v1/leave/requests/list?page=1&size=5
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
jsonpath "$.data.summary" isObject

GET {{base}}/api/v1/leave/requests/list?approval_stage=pending_final&status=Pending&page=1&size=5
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

GET {{base}}/api/v1/leave/requests/list?status=Approved&page=1&size=5
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true

GET {{base}}/api/v1/leave/requests/list?status=Rejected&page=1&size=5
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true

GET {{base}}/api/v1/leave/requests/list?search=kwame&page=1&size=5
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true

GET {{base}}/api/v1/leave/requests/list?from_date={{start_date}}&to_date={{end_date}}&page=1&size=5
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
