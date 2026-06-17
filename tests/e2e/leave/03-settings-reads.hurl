# Leave — settings reads (types, holidays, balances)

GET {{base}}/api/v1/leave/types/list?active_only=true&page=1&size=20
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
jsonpath "$.pagination.page" isInteger
jsonpath "$.pagination.total" isInteger

GET {{base}}/api/v1/leave/holidays/list?country=Ghana&year=true&page=1&size=10
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

GET {{base}}/api/v1/leave/balances/list?page=1&size=5
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
