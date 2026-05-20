-- Attendance
CREATE TABLE IF NOT EXISTS zeloshr.zhr_attendance_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    employee_code VARCHAR(32),
    department_name VARCHAR(150),
    branch_name VARCHAR(150),
    attendance_date DATE NOT NULL,
    clock_in TIME,
    clock_out TIME,
    status VARCHAR(50) NOT NULL,
    hours_worked NUMERIC(5,2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_zhr_attendance_date
    ON zeloshr.zhr_attendance_records (tenant_id, org_id, attendance_date DESC);

-- Leave
CREATE TABLE IF NOT EXISTS zeloshr.zhr_leave_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    leave_type VARCHAR(80) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    days_requested NUMERIC(4,1) NOT NULL,
    status VARCHAR(50) NOT NULL,
    approver_name VARCHAR(200),
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS zeloshr.zhr_leave_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    leave_type VARCHAR(80) NOT NULL,
    entitled_days NUMERIC(5,1) NOT NULL,
    used_days NUMERIC(5,1) NOT NULL DEFAULT 0,
    remaining_days NUMERIC(5,1) NOT NULL
);

-- Recruitment
CREATE TABLE IF NOT EXISTS zeloshr.zhr_job_postings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    title VARCHAR(200) NOT NULL,
    department_name VARCHAR(150),
    branch_name VARCHAR(150),
    employment_type VARCHAR(50),
    status VARCHAR(50) NOT NULL,
    applicants_count INT NOT NULL DEFAULT 0,
    posted_at DATE NOT NULL,
    closing_date DATE
);

-- Onboarding
CREATE TABLE IF NOT EXISTS zeloshr.zhr_onboarding_tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    task_name VARCHAR(200) NOT NULL,
    category VARCHAR(80) NOT NULL,
    due_date DATE NOT NULL,
    status VARCHAR(50) NOT NULL,
    assigned_to VARCHAR(200)
);

-- Performance
CREATE TABLE IF NOT EXISTS zeloshr.zhr_performance_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    review_period VARCHAR(100) NOT NULL,
    reviewer_name VARCHAR(200),
    overall_rating VARCHAR(50),
    status VARCHAR(50) NOT NULL,
    due_date DATE NOT NULL
);

-- Disciplinary
CREATE TABLE IF NOT EXISTS zeloshr.zhr_disciplinary_cases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    case_type VARCHAR(100) NOT NULL,
    severity VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL,
    opened_at DATE NOT NULL,
    description TEXT
);

-- Documents (employee documents module — not file binary store yet)
CREATE TABLE IF NOT EXISTS zeloshr.zhr_employee_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    employee_id UUID NOT NULL,
    employee_full_name VARCHAR(200) NOT NULL,
    document_name VARCHAR(255) NOT NULL,
    category VARCHAR(80) NOT NULL,
    file_size_kb INT NOT NULL,
    uploaded_by VARCHAR(200) NOT NULL,
    uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status VARCHAR(50) NOT NULL DEFAULT 'Active'
);
