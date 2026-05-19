Create a new md file in docs for each A, B, C, D and E, based on titles and create phases and stages based on below requirements:

A. App configuration:

Refactor and extend the current application to support a proper multi-tenant architecture using both Tenant and Campus concepts, without breaking any existing logic, database structure, or UI.

------------------------------------------
✅ CRITICAL RULES
------------------------------------------

- DO NOT break existing functionality
- DO NOT remove or rewrite existing modules
- DO NOT restructure current domain logic (School / College / University)
- DO NOT introduce random redesigns
- Extend the current system carefully and incrementally
- All changes must align with current architecture and naming conventions

------------------------------------------
✅ TARGET ARCHITECTURE
------------------------------------------

The system must support:

- Tenant (organization level, e.g., APS)
- Campus (sub-units under tenant)

Each tenant can have multiple campuses.

------------------------------------------
✅ REFERENCE STRUCTURE (FOR UNDERSTANDING ONLY)
------------------------------------------

The following example is ONLY for conceptual understanding.
DO NOT hardcode or build specifically for this exact structure.

Example:

Tenant: APS Organization
    - Campus: Lahore Campus
    - Campus: Karachi Campus
    - Campus: Islamabad Campus

This example is only to illustrate hierarchy and relationships.

------------------------------------------
✅ EXISTING SYSTEM CONSTRAINT (VERY IMPORTANT)
------------------------------------------

The system already supports:

- School
- College
- University

These are represented through:
- InstitutionType
- Departments
- Academic programs

You MUST:

- KEEP this structure unchanged
- INTEGRATE Tenant + Campus WITHIN this structure
- NOT replace or duplicate InstitutionType logic

------------------------------------------
✅ EXPECTED INTEGRATION MODEL
------------------------------------------

The final structure should behave like:

Tenant → Campus → InstitutionType → Departments → Programs → Users

Where:

- Tenant represents the customer organization
- Campus represents physical or logical locations
- InstitutionType continues to define School / College / University

------------------------------------------
✅ IMPLEMENTATION GUIDANCE (NO CODE)
------------------------------------------

1. Introduce Tenant and Campus as new domain layers
2. Link Campus under Tenant
3. Associate all existing data (users, departments, etc.) with both Tenant and Campus
4. Ensure compatibility with existing InstitutionType logic
5. Avoid duplicating data structures
6. Ensure every data record belongs to:
   - one Tenant
   - one Campus

------------------------------------------
✅ DATA SAFETY REQUIREMENTS
------------------------------------------

- Existing data must remain intact
- Existing users must continue working
- All current records should be safely assigned to a default Tenant and Campus
- No data loss or invalid state

------------------------------------------
✅ SYSTEM BEHAVIOR REQUIREMENTS
------------------------------------------

- Users only access data within their Tenant and Campus
- SuperAdmin can access across tenants and campuses
- Filtering must respect both Tenant and Campus
- No cross-tenant or cross-campus data leakage

------------------------------------------
✅ UI REQUIREMENTS
------------------------------------------

Add management interfaces:

- Tenant Management (for super admin)
- Campus Management (linked to tenant)

Ensure:
- Use existing UI layout and components
- Do NOT break visuals or design system
- Follow existing sidebar/menu patterns

------------------------------------------
✅ PERFORMANCE REQUIREMENTS
------------------------------------------

- Optimize data access using Tenant + Campus scoping
- Ensure no performance degradation
- Avoid unnecessary joins or complex queries

------------------------------------------
✅ FINAL GOAL
------------------------------------------

Transform the system into:

- A scalable multi-tenant SaaS platform
- Supporting multiple organizations (tenants)
- Each with multiple campuses
- Fully integrated with existing School / College / University structure
- Without breaking existing functionality, UI, or architecture

------------------------------------------

Focus on clean integration, system stability, and long-term scalability.
Do not generate unnecessary extra features or redesign existing modules.


B. CONFIGURATION + DEPLOYMENT:

Enhance the currently loaded application to fully support environment-based configuration management, focusing on safe and flexible database connection handling when the app is published, released, or deployed to customer environments.

------------------------------------------
✅ CRITICAL RULES
------------------------------------------

- DO NOT break existing functionality
- DO NOT hardcode any configuration values
- DO NOT remove current configuration system
- Build on the existing configuration approach
- Ensure backward compatibility
- Keep configuration secure and production-ready

------------------------------------------
✅ OBJECTIVE
------------------------------------------

Ensure the application properly supports ALL configuration scenarios for:

- Development (local)
- Testing / staging
- Production
- Customer deployments (on-prem or cloud)

------------------------------------------
✅ TASK 1: CONFIGURATION STRUCTURE
------------------------------------------

Ensure the app supports environment-specific configurations:

- Development
- Staging
- Production
- Custom customer environment

Use proper configuration hierarchy:

- Base configuration
- Environment-specific overrides

Ensure:
- Clean separation of concerns
- No duplicated configuration logic
- Proper fallback behavior

------------------------------------------
✅ TASK 2: DATABASE CONNECTION MANAGEMENT
------------------------------------------

Ensure flexible and secure database configuration:

Support:

- Local SQL Server (development)
- Managed cloud databases (Azure SQL, etc.)
- On-prem customer database servers

Requirements:

- Connection string must NOT be hardcoded
- Must support override via:
  - environment variables
  - configuration files
  - deployment settings

Ensure:
- Application auto-detects environment
- Loads correct connection string automatically

------------------------------------------
✅ TASK 3: SECURE CONFIGURATION HANDLING
------------------------------------------

Ensure sensitive configuration is handled securely:

- Use environment variables for secrets in production
- Avoid storing credentials in source code
- Support external configuration sources

Ensure:
- Passwords, keys, tokens are not exposed
- Safe handling in logs and errors

------------------------------------------
✅ TASK 4: DEPLOYMENT FLEXIBILITY
------------------------------------------

Ensure system supports:

1. Cloud deployment (e.g., Azure)
2. Customer-hosted deployment
3. Multi-instance environments

Configuration must allow:

- Different DB per customer
- Different domains per tenant
- Different scaling setups

------------------------------------------
✅ TASK 5: CUSTOMER DEPLOYMENT SUPPORT
------------------------------------------

Prepare system for real-world deployment:

- Allow customer-specific configuration without code changes
- Support configuration via:
  - app settings
  - environment variables
  - deployment pipeline

Ensure:
- Easy onboarding for new customers
- Minimal manual setup required

------------------------------------------
✅ TASK 6: TENANT + CAMPUS AWARE CONFIGURATION
------------------------------------------

Ensure configuration works with the multi-tenant system:

- Support per-tenant settings (future-ready)
- Allow isolation between tenants if required
- Avoid mixing tenant data via configuration

------------------------------------------
✅ TASK 7: FAIL-SAFE BEHAVIOR
------------------------------------------

If configuration is missing:

- Provide clear error messages
- Do NOT crash silently
- Fail early with meaningful logs

------------------------------------------
✅ TASK 8: PERFORMANCE AND STABILITY
------------------------------------------

- Avoid reloading configuration unnecessarily
- Cache configuration where appropriate
- Ensure no performance overhead

------------------------------------------
✅ TASK 9: LOGGING AND VISIBILITY
------------------------------------------

Ensure logs clearly indicate:

- Active environment
- Configuration source being used
- Database connection type (not credentials)

------------------------------------------
✅ FINAL GOAL
------------------------------------------

The system must be:

✅ Ready for development, staging, and production  
✅ Easily deployable to any customer  
✅ Fully configurable without code changes  
✅ Secure and scalable  
✅ Compatible with Tenant + Campus architecture


C. COURSE MATERIAL MODULE:

Extend the current system by adding a new feature "Course Material" while ensuring full compatibility with existing Tenant + Campus + InstitutionType (School / College / University) structure.

------------------------------------------
✅ CRITICAL RULES
------------------------------------------

- DO NOT break existing functionality
- DO NOT modify existing logic unnecessarily
- DO NOT change working modules
- DO NOT alter current UI design system
- Follow current architecture and coding patterns
- Ensure backward compatibility
- Ensure Tenant + Campus isolation is maintained

------------------------------------------
✅ FEATURE: COURSE MATERIAL MODULE
------------------------------------------

Add a new menu:

"Course Material"

------------------------------------------
✅ ACCESS CONTROL
------------------------------------------

- SuperAdmin → Full access (all tenants/campuses)
- Admin → Manage material for assigned departments
- Faculty → Upload/manage material for assigned courses
- Students → View, open links, download only

------------------------------------------
✅ FUNCTIONAL REQUIREMENTS
------------------------------------------

1. MATERIAL MANAGEMENT (UPLOAD)

Authorized users (SuperAdmin/Admin/Faculty) can:

- Upload course materials such as:
  - Links (URLs)
  - Books
  - Thesis
  - Documents

Material MUST be created using a structured selection sequence:

Step-by-step selection:

1. Select Department
2. Based on Department → show Courses
3. Based on Course → show Semesters
4. Based on Semester → show Subjects

IMPORTANT:

- Each dropdown must load data based on previous selection
- Avoid showing full lists (no global dropdowns)
- Use dependent filtering

------------------------------------------
✅ REQUIRED FIELDS

- Name of document/material → REQUIRED
- Department → REQUIRED
- Course → REQUIRED
- Semester → REQUIRED
- Subject → REQUIRED

------------------------------------------
✅ OPTIONAL FIELDS

- Link (URL)
- File Upload (document)
- Description

Users can:
- Upload file OR
- Provide link OR
- BOTH

------------------------------------------
✅ STORAGE REQUIREMENT

- Uploaded files must be stored in database or persistent storage system
- Must not be temporary
- Must always be accessible for students

------------------------------------------
✅ STUDENT ACCESS

Students can:

1. Select:
   - Department → Course → Semester → Subject

2. View:
   - Material list

3. Actions:
   - Open links
   - Download files

Students CANNOT:
- Edit
- Delete
- Upload

------------------------------------------
✅ MULTI-TENANT + CAMPUS REQUIREMENT
------------------------------------------

VERY IMPORTANT:

- Material must be isolated by:
  - Tenant
  - Campus

This ensures:

- Same Tenant → multiple campuses
- Materials are NOT shared across campuses unless explicitly allowed

------------------------------------------
✅ EXAMPLE (REFERENCE ONLY — DO NOT HARDCODE)
------------------------------------------

Tenant: APS Organization

Campus Lahore:
  - Physics Notes (visible only to Lahore campus)

Campus Karachi:
  - Physics Notes (separate data)

Even if names match:
- Data must NOT mix

------------------------------------------
✅ DATABASE REQUIREMENTS
------------------------------------------

Update database properly to support:

- Course material entity
- Relationship with:
  - Tenant
  - Campus
  - Department
  - Course
  - Semester
  - Subject

Ensure:

- All data is scoped by TenantId and CampusId
- Add proper indexes for performance
- Avoid duplication issues
- Support both file and link storage

------------------------------------------
✅ PERFORMANCE REQUIREMENTS
------------------------------------------

- Optimize queries using:
  - TenantId
  - CampusId
  - DepartmentId
  - CourseId
- No full table scans
- Use proper indexing

------------------------------------------
✅ UI REQUIREMENTS
------------------------------------------

- Add "Course Material" in sidebar menu
- Follow existing UI structure
- Use consistent layout and styling
- Do NOT break visuals

Pages required:

1. Manage Materials (Admin/Faculty)
2. View Materials (Students)

------------------------------------------
✅ SECURITY
------------------------------------------

- Ensure users only access material for:
  - their Tenant
  - their Campus
  - their assigned Department/Course

- Prevent cross-campus data leakage

------------------------------------------
✅ DATA SAFETY
------------------------------------------

- Do not affect existing data
- Do not modify unrelated tables incorrectly
- Ensure migration is safe

------------------------------------------
✅ FINAL GOAL
------------------------------------------

Add a fully functional Course Material system that:

✅ Supports uploads and links  
✅ Uses structured dependent filtering  
✅ Works with Tenant + Campus isolation  
✅ Integrates with current School/College/University structure  
✅ Maintains performance and scalability  
✅ Does NOT break the app  

------------------------------------------

Focus on clean integration, correct data relationships, and stability.
Do not introduce unnecessary redesign or complexity.


D. ANALYTICS & INTERACTIVE CHARTS:

Enhance the Analytical section by adding advanced, interactive charts with full support for interactive legends (user-controlled visibility of data).

--------------------------------------------------
✅ CRITICAL RULES
--------------------------------------------------

- DO NOT break existing functionality
- DO NOT modify business logic unnecessarily
- DO NOT redesign existing UI completely
- Use current UI structure and styling
- Maintain compatibility with Tenant + Campus architecture

--------------------------------------------------
✅ OBJECTIVE
--------------------------------------------------

Create a professional analytics dashboard with interactive charts that allow users to control what data is displayed.

--------------------------------------------------
✅ REQUIRED CHART TYPES
--------------------------------------------------

Include:

- Pie charts
- Bar graphs
- Line charts

Charts must represent:

- Student distribution
- Department-wise counts
- Course trends
- Semester/class performance
- Attendance trends (if available)
- Result trends (if available)

--------------------------------------------------
✅ INTERACTIVE LEGEND (VERY IMPORTANT)
--------------------------------------------------

Each chart MUST include an interactive legend (ledger-like behavior).

Users must be able to:

- Click on legend items to SHOW or HIDE specific data segments
- Toggle individual categories in charts (e.g., departments, courses)
- Dynamically update charts based on selected legend items

Behavior requirements:

- Pie charts:
  - Users can disable/enable slices
  - Chart updates to show only selected segments

- Bar/Line charts:
  - Users can toggle specific series (e.g., departments or semesters)
  - Unselected items should NOT appear visually

--------------------------------------------------
✅ REFERENCE EXAMPLE (FOR UNDERSTANDING ONLY)
--------------------------------------------------

This is only conceptual. Do NOT hardcode this structure.

Example:

Pie Chart: Student Distribution by Department

Legend:
- Computer Science
- Business
- Engineering

User selects only:
- Computer Science
- Engineering

Result:
- Pie chart shows ONLY these two
- Business is hidden

This applies to ALL charts.

--------------------------------------------------
✅ GLOBAL FILTER SYSTEM
--------------------------------------------------

Filters must be applied ONCE and affect ALL charts.

Filters include:

- Institution Type (School / College / University)
- Department
- Course / Program
- Semester / Class

--------------------------------------------------
✅ FILTER BEHAVIOR
--------------------------------------------------

- Filters must be dependent:

1. Institution Type
2. Department (filtered)
3. Course (filtered)
4. Semester/Class (filtered)

- Selecting a filter:
  - Updates ALL charts
  - Updates legend data accordingly

--------------------------------------------------
✅ MULTI-TENANT + CAMPUS REQUIREMENT
--------------------------------------------------

ALL charts must strictly use:

- TenantId
- CampusId

Ensure:

- No data leakage between campuses
- Same Tenant but different campuses remain isolated

--------------------------------------------------
✅ UI REQUIREMENTS
--------------------------------------------------

- Place charts inside structured layout (cards or panels)
- Keep alignment and spacing consistent
- Legends must be clearly visible and clickable
- Use color-coded categories

--------------------------------------------------
✅ USER EXPERIENCE REQUIREMENTS
--------------------------------------------------

- Charts must update instantly when:
  - Filters change
  - Legend selection changes

- Smooth transitions for updates
- No page reload required

--------------------------------------------------
✅ PERFORMANCE REQUIREMENTS
--------------------------------------------------

- No full dataset loading
- Use optimized queries
- Load only required data per filter

--------------------------------------------------
✅ SECURITY
--------------------------------------------------

- Users only see analytics for:
  - their Tenant
  - their Campus
  - their assigned institution data

--------------------------------------------------
✅ FINAL GOAL
--------------------------------------------------

Upgrade the Analytical section into a professional dashboard that:

✅ Supports interactive charts  
✅ Includes legend-based data control (toggle visibility)  
✅ Uses global filters applied across all charts  
✅ Respects Tenant + Campus isolation  
✅ Integrates with existing structure  
✅ Maintains performance and stability  

--------------------------------------------------

Focus on usability, interactivity, and clean integration.
Avoid static or non-interactive chart implementations.


E. MASTER SYSTEM VALIDATION PROMPT

Perform a FULL system validation of the current project after recent changes.

------------------------------------------
✅ OBJECTIVE
------------------------------------------

Verify that all new and existing features are stable, consistent, and working correctly.

------------------------------------------
✅ VALIDATION REQUIREMENTS
------------------------------------------

Check:

1. No existing functionality is broken
2. All modules work correctly end-to-end
3. No UI misalignment or layout issues exist
4. No missing data bindings or broken forms
5. APIs return correct and consistent responses
6. No null reference or runtime errors occur
7. Database relationships are valid and consistent

------------------------------------------
✅ MULTI-TENANT VALIDATION
------------------------------------------

Ensure:

- Tenant isolation is correct
- Campus isolation is correct
- No cross-tenant or cross-campus data leakage
- All queries respect TenantId and CampusId

------------------------------------------
✅ FEATURE VALIDATION
------------------------------------------

Verify:

- Course Material module works completely
- Analytics charts load correctly and update with filters
- Campus and Tenant management works properly
- Role-based access works correctly

------------------------------------------
✅ UI VALIDATION
------------------------------------------

- No overlapping elements
- Proper alignment and spacing
- Responsive layout
- All buttons and actions functional

------------------------------------------
✅ PERFORMANCE CHECK
------------------------------------------

- No slow or blocking queries
- Efficient filtering
- No unnecessary full dataset loads

------------------------------------------
✅ FINAL GOAL
------------------------------------------

Identify issues, inconsistencies, or risks and provide fixes without breaking the system.


✅ 2. DATABASE SAFETY & CONSISTENCY CHECK
👉 Prevents silent data corruption (VERY IMPORTANT)
Act as a senior database architect.

Audit the database schema, migrations, and relationships for consistency and safety.

------------------------------------------

Check:

- All tables correctly use TenantId and CampusId
- Foreign keys are valid and enforced
- Indexes exist for filtering columns
- No duplicate or conflicting constraints exist
- No nullable fields where they should not be
- Existing data integrity is maintained

------------------------------------------

Ensure:

- Queries are optimized
- No redundant indexes
- No risk of data inconsistency

------------------------------------------

Goal:

A stable, optimized, and future-proof database.


✅ 3. PERMISSION & ACCESS CONTROL AUDIT
👉 Prevents security leaks (critical for SaaS)
Act as a security and access control expert.

Audit role-based access across the system.

------------------------------------------

Ensure:

- SuperAdmin has full access
- Admin has scoped access
- Faculty limited to assigned courses
- Students have read-only access

------------------------------------------

Check:

- No unauthorized access to features
- No cross-tenant or cross-campus access
- Proper restrictions on API endpoints

------------------------------------------

Goal:

Strict, secure, and predictable access control.


✅ 4. UI CONSISTENCY & DESIGN SYSTEM CHECK
👉 Prevents your earlier UI break issue
Act as a senior UI/UX engineer.

Review all pages for design consistency.

------------------------------------------

Check:

- All pages use the same layout and grid system
- Consistent spacing, fonts, and styling
- No raw HTML elements without styling
- Buttons and components follow design system

------------------------------------------

Ensure:

- Sidebar alignment is correct
- Headers and content are properly structured
- No overlapping elements

------------------------------------------

Goal:

A clean, professional, and consistent interface.


✅ 5. PERFORMANCE & QUERY OPTIMIZATION CHECK
👉 Ensures your system scales later
Act as a performance optimization expert.

Analyze system performance and database queries.

------------------------------------------

Check:

- Queries filtered by TenantId and CampusId
- No unnecessary joins
- No full table scans
- Efficient pagination where needed

------------------------------------------

Ensure:

- Fast response times
- Scalable architecture
- Efficient analytics queries

------------------------------------------

Goal:

Optimized system ready for scale.


✅ 6. DEPLOYMENT READINESS CHECK (VERY IMPORTANT)
👉 Makes sure your app won’t fail on customer deployment
Act as a DevOps and deployment expert.

Prepare the system for production deployment.

------------------------------------------

Check:

- Environment-based configuration works
- Database connection is configurable
- No hardcoded values
- Proper error handling in production mode

------------------------------------------

Ensure:

- Logs are meaningful
- Missing configs fail safely
- System is deployable on different environments

------------------------------------------

Goal:

Production-ready, stable, and configurable system.


✅ 7. DATA FLOW & USER JOURNEY VALIDATION
👉 Ensures real users can actually use it smoothly
Act as a product and workflow expert.

Validate real user flows across the system.

------------------------------------------

Check:

- Student journey (login → view data → download material)
- Faculty journey (upload → manage → update material)
- Admin journey (manage campus, users, analytics)

------------------------------------------

Ensure:

- No blockers or broken steps
- Logical data flow
- Smooth navigation

------------------------------------------

Goal:

Usable, intuitive, and complete workflows.


✅ ✅ FINAL ADVICE (CRITICAL)
Now your workflow should be:
✅ After EACH major change:

Run Feature Prompt
Run Validation Prompt (#1)
Run DB Check (#2)
Run UI Fix (#4 if needed)