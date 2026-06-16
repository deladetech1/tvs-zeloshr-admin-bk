# Leave Management dashboard — flat widget contract (structure always; row shape when data exists)
# Pair with 01-reads.hurl. Row-level asserts skip gracefully when a widget is empty.

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
jsonpath "$.data.summary.on_leave_today" isInteger
jsonpath "$.data.summary.pending_approvals" isInteger
jsonpath "$.data.summary.leaving_this_week" isInteger
jsonpath "$.data.summary.low_balance_alert" isInteger
jsonpath "$.data.on_leave_today" isCollection
jsonpath "$.data.pending_approvals" isCollection
jsonpath "$.data.leaving_this_week" isCollection

# Leave Approvals page (View all — final queue)
GET {{base}}/api/v1/leave/approvals/list?tab=pending&page=1&size=20
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.data.pending_count" isInteger
jsonpath "$.data.items" isCollection

# Dashboard widget View all — all pending stages
GET {{base}}/api/v1/leave/requests/list?status=Pending&page=1&size=20
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
jsonpath "$.pagination.total" isInteger

# On leave today View all
GET {{base}}/api/v1/leave/requests/list?status=Approved&page=1&size=20
accept: application/json
app-id: {{app_id}}
authorization: Bearer {{token}}
bus-id: {{bus_id}}
loc-id: {{loc_id}}
org-id: {{org_id}}

HTTP 200
[Asserts]
jsonpath "$.success" == true
