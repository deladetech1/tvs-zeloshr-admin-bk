-- Attendance (today + recent)
INSERT INTO zeloshr.zhr_attendance_records (
    id, tenant_id, org_id, employee_id, employee_full_name, employee_code,
    department_name, branch_name, attendance_date, clock_in, clock_out, status, hours_worked
)
VALUES
    ('f1000001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000001', 'Ama Asante', 'ZEL-0042', 'Product', 'Accra HQ', CURRENT_DATE, '08:02', '17:15', 'Present', 8.5),
    ('f1000001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000004', 'Kofi Mensah', 'ZEL-0015', 'Frontend Engineering', 'Accra HQ', CURRENT_DATE, '08:45', '17:30', 'Late', 8.0),
    ('f1000001-0000-4000-8000-000000000003', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000002', 'Kwame Asare', 'ZEL-0089', 'Data & Insights', 'Accra HQ', CURRENT_DATE, NULL, NULL, 'Absent', 0),
    ('f1000001-0000-4000-8000-000000000004', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000005', 'Abena Owusu', 'ZEL-0033', 'Marketing', 'Kumasi', CURRENT_DATE, NULL, NULL, 'On Leave', 0),
    ('f1000001-0000-4000-8000-000000000005', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000010', 'Kwame Boateng', 'ZEL-0010', 'Engineering', 'Tema', CURRENT_DATE, '07:55', '16:00', 'Present', 8.0),
    ('f1000001-0000-4000-8000-000000000006', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000012', 'Kojo Annan', 'ZEL-0077', 'Finance', 'Kumasi', CURRENT_DATE - 1, '08:10', '17:00', 'Present', 8.0)
ON CONFLICT (id) DO NOTHING;

INSERT INTO zeloshr.zhr_leave_requests (
    id, tenant_id, org_id, employee_id, employee_full_name, leave_type,
    start_date, end_date, days_requested, status, approver_name
)
VALUES
    ('f2000001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000005', 'Abena Owusu', 'Annual Leave', CURRENT_DATE, CURRENT_DATE + 4, 5, 'Approved', 'Kwame Boateng'),
    ('f2000001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000001', 'Ama Asante', 'Annual Leave', CURRENT_DATE + 7, CURRENT_DATE + 9, 3, 'Pending', NULL),
    ('f2000001-0000-4000-8000-000000000003', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000004', 'Kofi Mensah', 'Sick Leave', CURRENT_DATE - 2, CURRENT_DATE - 1, 2, 'Approved', 'Kwame Boateng'),
    ('f2000001-0000-4000-8000-000000000004', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000011', 'Efua Boateng', 'Compassionate', CURRENT_DATE + 14, CURRENT_DATE + 16, 3, 'Pending', NULL)
ON CONFLICT (id) DO NOTHING;

INSERT INTO zeloshr.zhr_leave_balances (
    id, tenant_id, org_id, employee_id, employee_full_name, leave_type, entitled_days, used_days, remaining_days
)
VALUES
    ('f2100001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000001', 'Ama Asante', 'Annual Leave', 21, 5, 16),
    ('f2100001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000004', 'Kofi Mensah', 'Annual Leave', 21, 8, 13)
ON CONFLICT (id) DO NOTHING;

INSERT INTO zeloshr.zhr_job_postings (
    id, tenant_id, org_id, title, department_name, branch_name, employment_type, status, applicants_count, posted_at, closing_date
)
VALUES
    ('f3000001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'Senior Software Engineer', 'Engineering', 'Accra HQ', 'Full-time', 'Open', 24, CURRENT_DATE - 14, CURRENT_DATE + 30),
    ('f3000001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'Product Designer', 'Product Design', 'Accra HQ', 'Full-time', 'Open', 18, CURRENT_DATE - 7, CURRENT_DATE + 21),
    ('f3000001-0000-4000-8000-000000000003', 'demo-tenant', 'demo-org', 'Finance Analyst', 'Finance', 'Kumasi', 'Full-time', 'Closed', 42, CURRENT_DATE - 45, CURRENT_DATE - 5)
ON CONFLICT (id) DO NOTHING;

INSERT INTO zeloshr.zhr_onboarding_tasks (
    id, tenant_id, org_id, employee_id, employee_full_name, task_name, category, due_date, status, assigned_to
)
VALUES
    ('f4000001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000012', 'Kojo Annan', 'Complete IT setup', 'IT', CURRENT_DATE + 2, 'Pending', 'IT Support'),
    ('f4000001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000012', 'Kojo Annan', 'Sign employment contract', 'HR', CURRENT_DATE, 'In progress', 'Belinda Osei'),
    ('f4000001-0000-4000-8000-000000000003', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000012', 'Kojo Annan', 'Policy induction session', 'Training', CURRENT_DATE + 5, 'Pending', 'Serwa Acheampong')
ON CONFLICT (id) DO NOTHING;

INSERT INTO zeloshr.zhr_performance_reviews (
    id, tenant_id, org_id, employee_id, employee_full_name, review_period, reviewer_name, overall_rating, status, due_date
)
VALUES
    ('f5000001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000001', 'Ama Asante', 'H1 2025', 'Kwame Boateng', NULL, 'In progress', CURRENT_DATE + 14),
    ('f5000001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000004', 'Kofi Mensah', 'H1 2025', 'Ama Asante', 'Exceeds Expectations', 'Completed', CURRENT_DATE - 7),
    ('f5000001-0000-4000-8000-000000000003', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000010', 'Kwame Boateng', 'H1 2025', 'Esi Quainoo', NULL, 'Pending', CURRENT_DATE + 21)
ON CONFLICT (id) DO NOTHING;

INSERT INTO zeloshr.zhr_disciplinary_cases (
    id, tenant_id, org_id, employee_id, employee_full_name, case_type, severity, status, opened_at, description
)
VALUES
    ('f6000001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000002', 'Kwame Asare', 'Attendance violation', 'Medium', 'Open', CURRENT_DATE - 10, 'Repeated late arrivals in March.'),
    ('f6000001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000003', 'Yaw Appiah', 'Misconduct', 'High', 'Closed', CURRENT_DATE - 60, 'Resolved prior to termination.')
ON CONFLICT (id) DO NOTHING;

INSERT INTO zeloshr.zhr_employee_documents (
    id, tenant_id, org_id, employee_id, employee_full_name, document_name, category, file_size_kb, uploaded_by, uploaded_at, status
)
VALUES
    ('f7000001-0000-4000-8000-000000000001', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000001', 'Ama Asante', 'Employment Contract 2024.pdf', 'Contract', 840, 'Belinda Osei', NOW() - INTERVAL '30 days', 'Active'),
    ('f7000001-0000-4000-8000-000000000002', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000001', 'Ama Asante', 'Ghana Card Scan.jpg', 'National ID', 420, 'Belinda Osei', NOW() - INTERVAL '60 days', 'Active'),
    ('f7000001-0000-4000-8000-000000000003', 'demo-tenant', 'demo-org', 'e1000001-0000-4000-8000-000000000011', 'Efua Boateng', 'Fixed Term Contract.pdf', 'Contract', 1024, 'Fiifi Admin', NOW() - INTERVAL '14 days', 'Active')
ON CONFLICT (id) DO NOTHING;
