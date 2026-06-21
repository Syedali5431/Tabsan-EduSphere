# Tabsan EduSphere — Database Schema v1.0

> Generated from `01-Schema-Current.sql` | 91 tables | 2026-06-09

---

## 1. Identity & Access Management

### roles
System roles for role-based access control.

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Auto-increment role ID |
| Name | nvarchar(50) | Unique role name |
| Description | nvarchar(256) | Role description |
| IsSystemRole | bit | True for built-in roles |

### users
User accounts with role, tenant, campus, and institution scoping.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | User GUID |
| Username | nvarchar(100) | Unique login username |
| Email | nvarchar(256) | Email address (unique when not null) |
| FullName | nvarchar(200) | Full display name |
| FatherName | nvarchar(200) | Father's name |
| Address | nvarchar(500) | Physical address |
| PhoneNumber | nvarchar(32) | Contact phone number |
| ProfilePicturePath | nvarchar(500) | Profile picture relative path (e.g., uploads/profile-pictures/{guid}.jpg) |
| PasswordHash | nvarchar(512) | Argon2id password hash |
| RoleId | int (FK→roles.Id) | Assigned role |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Primary department |
| TenantId | uniqueidentifier (FK→tenants.Id) | Tenant scope |
| CampusId | uniqueidentifier (FK→campuses.Id) | Campus scope |
| InstitutionType | int | 0=School, 1=College, 2=University |
| ConsentToMonitoring | bit | ISO Phase 5 consent |
| DataRetentionDate | datetime2 | ISO Phase 5 retention date |
| IsActive | bit | Account active flag |
| LastLoginAt | datetime2 | Last successful login |
| CreatedAt | datetime2 | Record created |
| UpdatedAt | datetime2 | Last modified |
| RowVersion | rowversion | Concurrency token |
| IsDeleted | bit | Soft delete flag |
| DeletedAt | datetime2 | Deletion timestamp |

### user_sessions
JWT refresh token tracking.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Session GUID |
| UserId | uniqueidentifier (FK→users.Id) | Owning user |
| RefreshTokenHash | nvarchar(512) | Hashed refresh token |
| DeviceInfo | nvarchar(512) | User agent / device |
| IpAddress | nvarchar(64) | Client IP |
| ExpiresAt | datetime2 | Token expiry |
| RevokedAt | datetime2 | Revocation timestamp |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### password_history
Password change audit trail.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Record GUID |
| UserId | uniqueidentifier (FK→users.Id) | User |
| PasswordHash | nvarchar(512) | Historical hash |
| CreatedAt | datetime2 | When changed |

### consumed_verification_keys
One-time verification key tracking (password reset, MFA).

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Key GUID |
| KeyHash | nvarchar(64) | Hashed key |
| ConsumedAt | datetime2 | When used |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### login_activity_logs *(ISO Phase 3)*
Login attempt audit.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Log GUID |
| UserId | uniqueidentifier | User attempted |
| Username | nvarchar(100) | Username entered |
| AttemptedAt | datetime2 | When attempted |
| IpAddress | nvarchar(64) | Client IP |
| UserAgent | nvarchar(1024) | Browser agent |
| DeviceInfo | nvarchar(1024) | Device details |
| IsSuccess | bit | Login result |
| FailureReason | nvarchar(50) | Why failed |
| RiskLevel | nvarchar(20) | Risk assessment |
| UserIsLockedOut | bit | Lockout status |

---

## 2. Multi-Tenancy & Campus

### tenants
Organization/tenant records.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Tenant GUID |
| Code | nvarchar(64) | Unique tenant code |
| Name | nvarchar(200) | Display name |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### campuses
Campus locations per tenant.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Campus GUID |
| TenantId | uniqueidentifier (FK→tenants.Id) | Owning tenant |
| Code | nvarchar(64) | Unique campus code |
| Name | nvarchar(200) | Display name |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### buildings
Campus buildings.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Building GUID |
| TenantId | uniqueidentifier (FK→tenants.Id) | Tenant |
| CampusId | uniqueidentifier (FK→campuses.Id) | Campus |
| Name | nvarchar(100) | Building name |
| Code | nvarchar(20) | Building code |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### rooms
Classroom/resource rooms.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Room GUID |
| TenantId | uniqueidentifier (FK→tenants.Id) | Tenant |
| CampusId | uniqueidentifier (FK→campuses.Id) | Campus |
| Number | nvarchar(50) | Room number |
| BuildingId | uniqueidentifier (FK→buildings.Id) | Building |
| Capacity | int | Seating capacity |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

---

## 3. Academic Core

### departments
Academic departments (scoped by tenant/campus/institution type).

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Department GUID |
| Name | nvarchar(200) | Full name |
| Code | nvarchar(20) | Unique code |
| TenantId | uniqueidentifier | Tenant scope |
| CampusId | uniqueidentifier | Campus scope |
| InstitutionType | int | 0=School, 1=College, 2=University |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### admin_department_assignments
Admin-to-department access matrix.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Assignment GUID |
| AdminUserId | uniqueidentifier (FK→users.Id) | Admin user |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Department |
| AssignedAt | datetime2 | When assigned |
| RemovedAt | datetime2 | When removed |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### faculty_department_assignments
Faculty-to-department access matrix.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Assignment GUID |
| FacultyUserId | uniqueidentifier (FK→users.Id) | Faculty user |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Department |
| AssignedAt | datetime2 | When assigned |
| RemovedAt | datetime2 | When removed |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### academic_programs
Degree/program definitions.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Program GUID |
| Name | nvarchar(200) | Full program name |
| Code | nvarchar(20) | Unique program code |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Owning department |
| TotalSemesters | int | Total semesters/years |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### courses
Course master data with credit hours.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Course GUID |
| Title | nvarchar(200) | Course title |
| Code | nvarchar(20) | Unique course code |
| CreditHours | int | Credit/contact hours |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Owning department |
| TenantId | uniqueidentifier | Tenant scope |
| CampusId | uniqueidentifier | Campus scope |
| InstitutionType | int | Institution type |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### course_prerequisites
Course prerequisite dependencies.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Rule GUID |
| CourseId | uniqueidentifier (FK→courses.Id) | The course |
| PrerequisiteCourseId | uniqueidentifier (FK→courses.Id) | Required prerequisite |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### semesters
Academic term definitions.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Semester GUID |
| Name | nvarchar(100) | Semester name |
| StartDate | datetime2 | Term start |
| EndDate | datetime2 | Term end |
| IsClosed | bit | Closed flag |
| ClosedAt | datetime2 | When closed |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### course_offerings
Course sections per semester (links courses to semesters).

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Offering GUID |
| CourseId | uniqueidentifier (FK→courses.Id) | Course |
| SemesterId | uniqueidentifier (FK→semesters.Id) | Semester |
| FacultyUserId | uniqueidentifier (FK→users.Id) | Assigned faculty |
| MaxEnrollment | int | Max students |
| IsOpen | bit | Enrollment open flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### enrollments
Student-to-course-offering registration.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Enrollment GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| CourseOfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| EnrolledAt | datetime2 | When enrolled |
| DroppedAt | datetime2 | When dropped |
| Status | nvarchar(max) | Enrollment status |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### student_profiles
Student lifecycle (enrollment, CGPA, semester tracking).

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Profile GUID |
| UserId | uniqueidentifier (FK→users.Id) | User account |
| RegistrationNumber | nvarchar(50) | Unique registration number |
| ProgramId | uniqueidentifier (FK→academic_programs.Id) | Academic program |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Department |
| AdmissionDate | datetime2 | Date of admission |
| Cgpa | decimal(4,2) | Cumulative GPA |
| CurrentSemesterNumber | int | Current semester/class |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### registration_whitelist
Restricted enrollment control.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Entry GUID |
| IdentifierType | nvarchar(max) | Type of identifier |
| IdentifierValue | nvarchar(256) | Identifier value |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Department |
| ProgramId | uniqueidentifier (FK→academic_programs.Id) | Program |
| IsUsed | bit | Whether consumed |
| UsedAt | datetime2 | When used |
| CreatedUserId | uniqueidentifier | Created by |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### school_streams
Academic streams (e.g., Science, Arts).

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Stream GUID |
| Name | nvarchar(120) | Stream name |
| Description | nvarchar(500) | Description |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### student_stream_assignments
Student-to-stream assignments.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Assignment GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| SchoolStreamId | uniqueidentifier (FK→school_streams.Id) | Stream |
| AssignedAt | datetime2 | When assigned |
| AssignedByUserId | uniqueidentifier | Who assigned |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### academic_deadlines
Academic deadline tracking.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Deadline GUID |
| SemesterId | uniqueidentifier (FK→semesters.Id) | Semester |
| Title | nvarchar(200) | Deadline title |
| Description | nvarchar(1000) | Details |
| DeadlineDate | datetime2 | Due date |
| ReminderDaysBefore | int | Days before to remind |
| IsActive | bit | Active flag |
| LastReminderSentAt | datetime2 | Last reminder sent |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

---

## 4. LMS (Learning Management System)

### course_announcements
Course-level announcements.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Announcement GUID |
| OfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| AuthorId | uniqueidentifier (FK→users.Id) | Author |
| Title | nvarchar(300) | Announcement title |
| Body | nvarchar(max) | Content |
| PostedAt | datetime2 | When posted |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### course_content_modules
Weekly/themed content modules.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Module GUID |
| OfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| Title | nvarchar(300) | Module title |
| WeekNumber | int | Week number |
| Body | nvarchar(max) | Content body |
| IsPublished | bit | Published flag |
| PublishedAt | datetime2 | When published |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### content_videos
Learning video resources.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Video GUID |
| ModuleId | uniqueidentifier (FK→course_content_modules.Id) | Parent module |
| Title | nvarchar(300) | Video title |
| StorageUrl | nvarchar(1000) | Storage URL |
| EmbedUrl | nvarchar(1000) | Embed URL |
| DurationSeconds | int | Video duration |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### discussion_threads
Q&A and discussion forums.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Thread GUID |
| OfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| Title | nvarchar(500) | Thread title |
| AuthorId | uniqueidentifier (FK→users.Id) | Author |
| ThreadType | nvarchar(50) | Type: Issue/Question/Discussion |
| IssueSubType | nvarchar(100) | Sub-type for issues |
| IsPinned | bit | Pinned flag |
| IsClosed | bit | Closed flag |
| IsSolved | bit | Solved flag |
| IsVisibleToAll | bit | Public visibility |
| TicketNumber | nvarchar(100) | Ticket number |
| ResolvedBy | uniqueidentifier | Who resolved |
| ResolvedAt | datetime2 | When resolved |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### discussion_replies
Thread replies.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Reply GUID |
| ThreadId | uniqueidentifier (FK→discussion_threads.Id) | Parent thread |
| AuthorId | uniqueidentifier (FK→users.Id) | Author |
| Body | nvarchar(max) | Reply content |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

---

## 5. Assessment & Grading

### assignments
Assignment definitions per course offering.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Assignment GUID |
| CourseOfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| Title | nvarchar(300) | Assignment title |
| Description | nvarchar(4000) | Instructions |
| DueDate | datetime2 | Deadline |
| MaxMarks | decimal(8,2) | Maximum marks |
| IsPublished | bit | Published flag |
| PublishedAt | datetime2 | When published |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### assignment_submissions
Student assignment submissions with grading.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Submission GUID |
| AssignmentId | uniqueidentifier (FK→assignments.Id) | Assignment |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| FileUrl | nvarchar(2048) | Uploaded file URL |
| TextContent | nvarchar(max) | Text submission |
| SubmittedAt | datetime2 | When submitted |
| MarksAwarded | decimal(8,2) | Grade awarded |
| Feedback | nvarchar(2000) | Teacher feedback |
| GradedAt | datetime2 | When graded |
| GradedByUserId | uniqueidentifier | Who graded |
| Status | nvarchar(max) | Submission status |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### quizzes
Quiz/test definitions.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Quiz GUID |
| CourseOfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| Title | nvarchar(300) | Quiz title |
| Instructions | nvarchar(4000) | Instructions |
| TimeLimitMinutes | int | Time limit |
| MaxAttempts | int | Max attempts allowed |
| AvailableFrom | datetime2 | Open from |
| AvailableUntil | datetime2 | Open until |
| IsPublished | bit | Published flag |
| IsActive | bit | Active flag |
| CreatedByUserId | uniqueidentifier | Creator |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### quiz_questions
Quiz questions.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Question GUID |
| QuizId | uniqueidentifier (FK→quizzes.Id) | Parent quiz |
| Text | nvarchar(2000) | Question text |
| Type | nvarchar(20) | Question type |
| Marks | decimal(8,2) | Marks for this question |
| OrderIndex | int | Display order |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### quiz_options
Multiple choice options.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Option GUID |
| QuizQuestionId | uniqueidentifier (FK→quiz_questions.Id) | Parent question |
| Text | nvarchar(1000) | Option text |
| IsCorrect | bit | Correct answer flag |
| OrderIndex | int | Display order |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### quiz_attempts
Quiz attempt tracking.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Attempt GUID |
| QuizId | uniqueidentifier (FK→quizzes.Id) | Quiz |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| StartedAt | datetime2 | When started |
| FinishedAt | datetime2 | When finished |
| Status | nvarchar(20) | Attempt status |
| TotalScore | decimal(10,2) | Total score |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### quiz_answers
Student quiz responses.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Answer GUID |
| QuizAttemptId | uniqueidentifier (FK→quiz_attempts.Id) | Attempt |
| QuizQuestionId | uniqueidentifier (FK→quiz_questions.Id) | Question |
| SelectedOptionId | uniqueidentifier | Selected option |
| TextResponse | nvarchar(4000) | Text answer |
| MarksAwarded | decimal(8,2) | Marks earned |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### results
Final grades per student per course offering.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Result GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| CourseOfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| ResultType | nvarchar(450) | Final/Midterm/Sessional/Practical/Internal |
| MarksObtained | decimal(8,2) | Earned marks |
| MaxMarks | decimal(8,2) | Maximum marks |
| IsPublished | bit | Published flag |
| PublishedAt | datetime2 | When published |
| PublishedByUserId | uniqueidentifier | Who published |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### rubrics
Assessment rubrics.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Rubric GUID |
| AssignmentId | uniqueidentifier (FK→assignments.Id) | Assignment |
| Title | nvarchar(300) | Rubric title |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### rubric_criteria
Rubric scoring criteria.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Criterion GUID |
| RubricId | uniqueidentifier (FK→rubrics.Id) | Parent rubric |
| Name | nvarchar(300) | Criterion name |
| MaxPoints | decimal(8,2) | Max points |
| DisplayOrder | int | Display order |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### rubric_levels
Rubric performance levels.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Level GUID |
| CriterionId | uniqueidentifier (FK→rubric_criteria.Id) | Criterion |
| Label | nvarchar(200) | Level label |
| PointsAwarded | decimal(8,2) | Points for this level |
| DisplayOrder | int | Display order |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### rubric_student_grades
Student rubric grades.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Grade GUID |
| AssignmentSubmissionId | uniqueidentifier (FK→assignment_submissions.Id) | Submission |
| RubricCriterionId | uniqueidentifier (FK→rubric_criteria.Id) | Criterion |
| RubricLevelId | uniqueidentifier (FK→rubric_levels.Id) | Awarded level |
| PointsAwarded | decimal(8,2) | Points earned |
| GradedByUserId | uniqueidentifier | Who graded |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### gpa_scale_rules
Institution-specific GPA calculation rules.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Rule GUID |
| InstitutionType | int | 0=School, 1=College, 2=University |
| GradePoint | decimal(4,2) | GPA point value |
| MinimumScore | decimal(5,2) | Minimum score for this grade |
| DisplayOrder | int | Display order |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### result_component_rules
Weighted result components (institution-scoped).

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Rule GUID |
| InstitutionType | int | Institution type |
| Name | nvarchar(100) | Component name |
| Weightage | decimal(5,2) | Weight percentage |
| DisplayOrder | int | Display order |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### course_grading_configs
Per-course grading profiles.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Config GUID |
| CourseId | uniqueidentifier (FK→courses.Id) | Course |
| PassThreshold | decimal(5,2) | Pass mark |
| GradingType | nvarchar(20) | Grading type |
| GradeRangesJson | nvarchar(4000) | Grade ranges JSON |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### institution_grading_profiles
Institution-specific grading standards.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Profile GUID |
| InstitutionType | int | Institution type |
| PassThreshold | decimal(5,2) | Pass threshold |
| GradeRangesJson | nvarchar(max) | Grade ranges JSON |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

---

## 6. Attendance & Timetabling

### attendance_records
Daily attendance marking.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Record GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| CourseOfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| Date | datetime2 | Attendance date |
| Status | nvarchar(20) | Present/Absent/Late |
| MarkedByUserId | uniqueidentifier | Who marked |
| Remarks | nvarchar(500) | Notes |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### timetables
Class schedules by department and semester.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Timetable GUID |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Department |
| SemesterId | uniqueidentifier (FK→semesters.Id) | Semester |
| AcademicProgramId | uniqueidentifier (FK→academic_programs.Id) | Program |
| EffectiveDate | date | Effective from |
| SemesterNumber | int | Semester number |
| IsPublished | bit | Published flag |
| PublishedAt | datetime2 | When published |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### timetable_entries
Individual class sessions with room/time.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Entry GUID |
| TimetableId | uniqueidentifier (FK→timetables.Id) | Parent timetable |
| DayOfWeek | int | Day of week (0=Sun) |
| StartTime | time | Start time |
| EndTime | time | End time |
| SubjectName | nvarchar(200) | Subject name |
| RoomNumber | nvarchar(50) | Room number |
| FacultyName | nvarchar(200) | Faculty name |
| CourseOfferingId | uniqueidentifier (FK→course_offerings.Id) | Course offering |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

---

## 7. FYP (Final Year Projects)

### fyp_projects
Student research projects.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Project GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Department |
| Title | nvarchar(500) | Project title |
| Description | nvarchar(max) | Description |
| Status | nvarchar(20) | InProgress/Completed/Approved |
| SupervisorUserId | uniqueidentifier (FK→users.Id) | Supervisor |
| CoordinatorRemarks | nvarchar(2000) | Coordinator notes |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### fyp_meetings
Project advisor meetings.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Meeting GUID |
| FypProjectId | uniqueidentifier (FK→fyp_projects.Id) | Project |
| ScheduledAt | datetime2 | When scheduled |
| Venue | nvarchar(500) | Meeting venue |
| Agenda | nvarchar(4000) | Agenda |
| Status | nvarchar(20) | Meeting status |
| OrganiserUserId | uniqueidentifier | Organiser |
| Minutes | nvarchar(max) | Meeting minutes |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### fyp_panel_members
Evaluation panel members.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Member GUID |
| FypProjectId | uniqueidentifier (FK→fyp_projects.Id) | Project |
| UserId | uniqueidentifier (FK→users.Id) | Panel member |
| Role | nvarchar(20) | Role on panel |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

---

## 8. Graduation & Certificates

### degree_rules
University degree completion requirements.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Rule GUID |
| AcademicProgramId | uniqueidentifier (FK→academic_programs.Id) | Program |
| MinTotalCredits | int | Minimum total credits |
| MinCoreCredits | int | Minimum core credits |
| MinElectiveCredits | int | Minimum elective credits |
| MinGpa | decimal(4,2) | Minimum CGPA required |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### degree_rule_required_courses
Courses required by degree rules.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Entry GUID |
| DegreeRuleId | uniqueidentifier (FK→degree_rules.Id) | Degree rule |
| CourseId | uniqueidentifier (FK→courses.Id) | Required course |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### graduation_applications
Student graduation requests.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Application GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| Status | int | Application status |
| StudentNote | nvarchar(2000) | Student note |
| SubmittedAt | datetime2 | When submitted |
| CertificatePath | nvarchar(500) | Certificate file path |
| CertificateGeneratedAt | datetime2 | When generated |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### graduation_application_approvals
Multi-stage approval workflow.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Approval GUID |
| GraduationApplicationId | uniqueidentifier (FK→graduation_applications.Id) | Application |
| Stage | int | Approval stage |
| ApproverUserId | uniqueidentifier (FK→users.Id) | Approver |
| IsApproved | bit | Approved flag |
| Note | nvarchar(1000) | Approval note |
| ActedAt | datetime2 | When acted |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### accreditation_templates
Accreditation framework mappings.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Template GUID |
| Name | nvarchar(200) | Template name |
| Description | nvarchar(500) | Description |
| Format | nvarchar(10) | Output format |
| FieldMappingsJson | nvarchar(2000) | Field mappings |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### transcript_export_logs
Academic transcript request audit.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Log GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| RequestedByUserId | uniqueidentifier | Who requested |
| ExportedAt | datetime2 | When exported |
| DocumentUrl | nvarchar(2048) | Document location |
| Format | nvarchar(10) | Export format |
| IpAddress | nvarchar(45) | Client IP |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### student_report_cards
Period-based report cards.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Report card GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| InstitutionType | int | Institution type |
| PeriodLabel | nvarchar(80) | Period label |
| PayloadJson | nvarchar(max) | Report data JSON |
| GeneratedByUserId | uniqueidentifier | Who generated |
| GeneratedAt | datetime2 | When generated |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

---

## 9. Academic Progression

### study_plans
Student-planned course paths.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Plan GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| PlannedSemesterName | nvarchar(100) | Semester name |
| Notes | nvarchar(2000) | Student notes |
| AdvisorStatus | int | Advisor review status |
| AdvisorNotes | nvarchar(2000) | Advisor comments |
| ReviewedByUserId | uniqueidentifier | Who reviewed |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### study_plan_courses
Courses in study plans.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Entry GUID |
| StudyPlanId | uniqueidentifier (FK→study_plans.Id) | Study plan |
| CourseId | uniqueidentifier (FK→courses.Id) | Course |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### bulk_promotion_batches
Bulk semester advancement batches.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Batch GUID |
| Title | nvarchar(180) | Batch title |
| Status | int | Batch status |
| CreatedByUserId | uniqueidentifier | Creator |
| ApprovedByUserId | uniqueidentifier | Approver |
| ReviewedAt | datetime2 | When reviewed |
| AppliedAt | datetime2 | When applied |
| ReviewNote | nvarchar(1000) | Review note |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### bulk_promotion_entries
Individual promotion records.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Entry GUID |
| BatchId | uniqueidentifier (FK→bulk_promotion_batches.Id) | Parent batch |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| Decision | int | Promotion decision |
| Reason | nvarchar(500) | Decision reason |
| IsApplied | bit | Applied flag |
| AppliedAt | datetime2 | When applied |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

---

## 10. Financial Management

### payment_receipts
Payment/fee tracking with receipt numbers.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Receipt GUID |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| CreatedByUserId | uniqueidentifier | Creator |
| ReceiptNo | nvarchar(64) | Unique receipt number |
| Status | int | Payment status |
| Amount | decimal(10,2) | Amount |
| Description | nvarchar(500) | Payment description |
| DueDate | datetime2 | Due date |
| ProofOfPaymentPath | nvarchar(500) | Proof file path |
| ProofUploadedAt | datetime2 | When proof uploaded |
| ConfirmedByUserId | uniqueidentifier | Who confirmed |
| ConfirmedAt | datetime2 | When confirmed |
| Notes | nvarchar(2000) | Additional notes |
| SemesterId | uniqueidentifier | Semester |
| CourseId | uniqueidentifier | Course |
| LevelNumber | int | Level/class |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### parent_student_links
Parent account links to student records.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Link GUID |
| ParentUserId | uniqueidentifier (FK→users.Id) | Parent user |
| StudentProfileId | uniqueidentifier (FK→student_profiles.Id) | Student |
| Relationship | nvarchar(60) | Relationship type |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

---

## 11. Communication & Support

### notifications
System/academic notifications.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Notification GUID |
| Title | nvarchar(300) | Notification title |
| Body | nvarchar(4000) | Content |
| Type | nvarchar(50) | Notification type |
| SenderUserId | uniqueidentifier | Sender |
| IsSystemGenerated | bit | System generated flag |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### notification_recipients
Per-user notification inbox.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Recipient GUID |
| NotificationId | uniqueidentifier (FK→notifications.Id) | Notification |
| RecipientUserId | uniqueidentifier (FK→users.Id) | Recipient |
| IsRead | bit | Read flag |
| ReadAt | datetime2 | When read |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### support_tickets
Helpdesk ticket management.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Ticket GUID |
| SubmitterId | uniqueidentifier (FK→users.Id) | Submitter |
| DepartmentId | uniqueidentifier (FK→departments.Id) | Department |
| Category | int | Ticket category |
| Subject | nvarchar(300) | Ticket subject |
| Body | nvarchar(4000) | Ticket body |
| Status | int | Ticket status |
| AssignedToId | uniqueidentifier | Assigned agent |
| ResolvedAt | datetime2 | When resolved |
| ReopenWindowDays | int | Reopen window in days |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### support_ticket_messages
Ticket conversations.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Message GUID |
| TicketId | uniqueidentifier (FK→support_tickets.Id) | Parent ticket |
| AuthorId | uniqueidentifier (FK→users.Id) | Author |
| Body | nvarchar(4000) | Message body |
| IsInternalNote | bit | Internal note flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### outbound_email_logs
Email delivery audit.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Log GUID |
| ToAddress | nvarchar(256) | Recipient email |
| Subject | nvarchar(500) | Email subject |
| Status | nvarchar(20) | Delivery status |
| ErrorMessage | nvarchar(2000) | Error if failed |
| AttemptedAt | datetime2 | When attempted |

---

## 12. AI & Analytics

### chat_conversations
AI chatbot sessions.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Conversation GUID |
| UserId | uniqueidentifier (FK→users.Id) | User |
| UserRole | nvarchar(50) | User's role |
| DepartmentId | uniqueidentifier | Department |
| StartedAt | datetime2 | When started |

### chat_messages
Chat message history with token usage.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Message GUID |
| ConversationId | uniqueidentifier (FK→chat_conversations.Id) | Conversation |
| Role | nvarchar(20) | user/assistant/system |
| Content | nvarchar(max) | Message content |
| SentAt | datetime2 | When sent |
| TokensUsed | int | Token count |

---

## 13. Change Management

### admin_change_requests
Admin modification requests.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Request GUID |
| RequestorUserId | uniqueidentifier | Who requested |
| ReviewedByUserId | uniqueidentifier | Who reviewed |
| Status | int | Request status |
| ChangeDescription | nvarchar(500) | What changed |
| Reason | nvarchar(2000) | Why |
| NewData | NVARCHAR(MAX) | New data JSON |
| AdminNotes | nvarchar(2000) | Admin notes |
| ReviewedAt | datetime2 | When reviewed |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### teacher_modification_requests
Teacher data correction requests.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Request GUID |
| TeacherUserId | uniqueidentifier | Teacher |
| ReviewedByUserId | uniqueidentifier | Reviewer |
| ModificationType | int | Type of change |
| RecordId | uniqueidentifier | Affected record |
| Status | int | Request status |
| Reason | nvarchar(2000) | Justification |
| ProposedData | NVARCHAR(MAX) | Proposed new data |
| AdminNotes | nvarchar(2000) | Admin notes |
| ReviewedAt | datetime2 | When reviewed |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

---

## 14. Platform Configuration

### modules
Feature modules.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Module GUID |
| Key | nvarchar(50) | Unique module key |
| Name | nvarchar(100) | Display name |
| IsMandatory | bit | Mandatory flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### module_status
Module activation status.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Status GUID |
| ModuleId | uniqueidentifier (FK→modules.Id) | Module |
| IsActive | bit | Active flag |
| ActivatedAt | datetime2 | When activated |
| Source | nvarchar(20) | Activation source |
| ChangedBy | uniqueidentifier | Who changed |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### module_role_assignments
Role-to-module access matrix.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Assignment GUID |
| ModuleId | uniqueidentifier (FK→modules.Id) | Module |
| RoleName | nvarchar(50) | Role name |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### portal_settings
Portal configuration (university name, theme, timezone).

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Setting GUID |
| Key | nvarchar(100) | Setting key |
| Value | nvarchar(1000) | Setting value |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### sidebar_menu_items
Navigation menu structure.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Menu GUID |
| Name | nvarchar(150) | Display name |
| Purpose | nvarchar(500) | Menu purpose |
| Key | nvarchar(100) | Unique menu key |
| ParentId | uniqueidentifier | Parent menu |
| DisplayOrder | int | Sort order |
| IsActive | bit | Active flag |
| IsSystemMenu | bit | System menu flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### sidebar_menu_role_accesses
Role-based menu visibility.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Access GUID |
| SidebarMenuItemId | uniqueidentifier (FK→sidebar_menu_items.Id) | Menu item |
| RoleName | nvarchar(100) | Role name |
| IsAllowed | bit | Allowed flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### role_resource_permissions
Fine-grained action permissions.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Permission GUID |
| RoleName | nvarchar(100) | Role name |
| ResourceKey | nvarchar(100) | Resource key |
| CanView | bit | View permission |
| CanAdd | bit | Add permission |
| CanEdit | bit | Edit permission |
| CanDeactivate | bit | Deactivate permission |
| CanExport | bit | Export permission |
| CanImport | bit | Import permission |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |

### report_definitions
Available reports.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Report GUID |
| Name | nvarchar(150) | Report name |
| Purpose | nvarchar(500) | Report purpose |
| Key | nvarchar(100) | Unique report key |
| IsActive | bit | Active flag |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |
| IsDeleted | bit | Soft delete |
| DeletedAt | datetime2 | Deleted at |

### report_role_assignments
Report access by role.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Assignment GUID |
| ReportDefinitionId | uniqueidentifier (FK→report_definitions.Id) | Report |
| RoleName | nvarchar(50) | Role name |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

### license_state
License validation and expiry.

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | License GUID |
| LicenseHash | nvarchar(128) | License hash |
| LicenseType | nvarchar(max) | License type |
| Status | nvarchar(max) | License status |
| ActivatedAt | datetime2 | When activated |
| ExpiresAt | datetime2 | Expiry date |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |
| RowVersion | rowversion | Concurrency |

---

## 15. Audit & Logging

### audit_logs
Activity audit logging.

| Column | Type | Description |
|--------|------|-------------|
| Id | bigint (PK) | Auto-increment log ID |
| ActorUserId | uniqueidentifier | Who performed action |
| Action | nvarchar(100) | Action name |
| EntityName | nvarchar(100) | Entity type |
| EntityId | nvarchar(100) | Entity identifier |
| OldValuesJson | nvarchar(max) | Previous state JSON |
| NewValuesJson | nvarchar(max) | New state JSON |
| OccurredAt | datetime2 | When occurred |
| IpAddress | nvarchar(64) | Client IP |

---

## 16. ISO Compliance Tables *(ISO Phases 4–10)*

### backup_logs *(ISO Phase 4 — Backup & DR)*

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Log GUID |
| BackupType | nvarchar(20) | Backup type |
| FileName | nvarchar(500) | File name |
| FilePath | nvarchar(1000) | File path |
| FileSizeBytes | bigint | File size |
| DurationSeconds | int | Duration |
| Status | nvarchar(20) | Backup status |
| StartedAt | datetime2 | Start time |
| CompletedAt | datetime2 | End time |
| ErrorMessage | nvarchar(2000) | Error if failed |
| Checksum | nvarchar(128) | File checksum |
| InitiatedBy | nvarchar(100) | Who initiated |

### data_classification_entries *(ISO Phase 5 — Data Protection)*

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Entry GUID |
| EntityName | nvarchar(100) | Entity name |
| EntityId | nvarchar(100) | Entity ID |
| ClassificationLevel | nvarchar(20) | Public/Internal/Confidential/Restricted |
| ClassifiedBy | uniqueidentifier | Who classified |
| ClassifiedAt | datetime2 | When classified |
| Justification | nvarchar(500) | Reason |

### incident_logs *(ISO Phase 6 — Incident Management)*

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Incident GUID |
| Title | nvarchar(300) | Incident title |
| Description | nvarchar(4000) | Details |
| Severity | nvarchar(20) | Low/Medium/High/Critical |
| Category | nvarchar(30) | Incident category |
| Status | nvarchar(20) | Open/Investigating/Resolved/Closed |
| ReportedBy | uniqueidentifier | Reporter |
| ReportedAt | datetime2 | When reported |
| AssignedTo | uniqueidentifier | Assignee |
| ResolvedAt | datetime2 | When resolved |
| Resolution | nvarchar(2000) | Resolution notes |

### policy_documents *(ISO Phase 7 — Document Management)*

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Document GUID |
| Title | nvarchar(300) | Document title |
| Description | nvarchar(2000) | Description |
| Content | nvarchar(max) | Document content |
| Version | int | Current version |
| Status | nvarchar(20) | Draft/Published/Archived |
| Category | nvarchar(50) | Document category |
| AccessLevel | nvarchar(20) | Public/Internal/Restricted |
| PublishedAt | datetime2 | When published |
| ArchivedAt | datetime2 | When archived |
| CreatedAt | datetime2 | Created |
| UpdatedAt | datetime2 | Updated |

### policy_document_versions *(ISO Phase 7)*

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Version GUID |
| DocumentId | uniqueidentifier (FK→policy_documents.Id) | Document |
| VersionNumber | int | Version number |
| Content | nvarchar(max) | Version content |
| ChangedBy | uniqueidentifier | Who changed |
| ChangedAt | datetime2 | When changed |
| ChangeNotes | nvarchar(1000) | Change description |

### backup_verification_logs *(ISO Phase 8 — Backup Validation)*

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier (PK) | Log GUID |
| BackupLogId | uniqueidentifier (FK→backup_logs.Id) | Backup log |
| VerificationType | nvarchar(20) | Verification type |
| VerifiedAt | datetime2 | When verified |
| VerifiedBy | nvarchar(100) | Who verified |
| IsSuccessful | bit | Success flag |
| DurationSeconds | int | Duration |
| Issues | nvarchar(2000) | Issues found |
| VerifiedChecksum | nvarchar(128) | Verified checksum |

---

## 17. Metadata

### __EFMigrationsHistory
Entity Framework Core migration tracking.

| Column | Type | Description |
|--------|------|-------------|
| MigrationId | nvarchar(150) (PK) | Migration identifier |
| ProductVersion | nvarchar(32) | EF Core version |

---

## Entity Relationship Summary

```
roles ──┐
         ├── users ──┬── student_profiles ──┬── enrollments
         │            │                      ├── attendance_records
         │            │                      ├── results
         │            │                      ├── fyp_projects ──┬── fyp_meetings
         │            │                      │                   └── fyp_panel_members
         │            │                      ├── assignment_submissions
         │            │                      ├── quiz_attempts ── quiz_answers
         │            │                      ├── study_plans ── study_plan_courses
         │            │                      ├── graduation_applications ── graduation_application_approvals
         │            │                      ├── payment_receipts
         │            │                      ├── transcript_export_logs
         │            │                      └── student_report_cards
         │            │
         │            ├── user_sessions
         │            ├── password_history
         │            └── login_activity_logs
         │
tenants ── campuses ──┬── departments ──┬── academic_programs
                       │                 ├── courses ──┬── course_offerings ──┬── assignments
                       │                 │             │                      ├── quizzes ── quiz_questions ── quiz_options
                       │                 │             │                      ├── course_announcements
                       │                 │             │                      ├── course_content_modules ── content_videos
                       │                 │             │                      ├── discussion_threads ── discussion_replies
                       │                 │             │                      └── enrollments
                       │                 │             └── course_prerequisites
                       │                 ├── buildings ── rooms
                       │                 ├── timetables ── timetable_entries
                       │                 └── faculty_department_assignments
                       │
                       └── semesters ──┬── course_offerings
                                       └── academic_deadlines

modules ── module_status
        └── module_role_assignments

sidebar_menu_items ── sidebar_menu_role_accesses

reports ── report_role_assignments

license_state (standalone)

audit_logs (cross-cutting)

admin_change_requests / teacher_modification_requests (workflow)

backup_logs ── backup_verification_logs
policy_documents ── policy_document_versions
data_classification_entries / incident_logs (ISO compliance)
```

---

```
```

---

## A. Foreign Key Constraints (73)

| Constraint Name | Child Table | FK Column | Parent Table | Parent Column | ON DELETE |
|---|---|---|---|---|---|
| FK_module_status_modules_ModuleId | module_status | ModuleId | modules | Id | CASCADE |
| FK_users_roles_RoleId | users | RoleId | roles | Id | NO ACTION |
| FK_user_sessions_users_UserId | user_sessions | UserId | users | Id | CASCADE |
| FK_campuses_tenants_TenantId | campuses | TenantId | tenants | Id | NO ACTION |
| FK_users_tenants_TenantId | users | TenantId | tenants | Id | NO ACTION |
| FK_departments_tenants_TenantId | departments | TenantId | tenants | Id | NO ACTION |
| FK_academic_programs_departments_DepartmentId | academic_programs | DepartmentId | departments | Id | NO ACTION |
| FK_courses_departments_DepartmentId | courses | DepartmentId | departments | Id | NO ACTION |
| FK_faculty_department_assignments_departments_DepartmentId | faculty_department_assignments | DepartmentId | departments | Id | NO ACTION |
| FK_student_profiles_academic_programs_ProgramId | student_profiles | ProgramId | academic_programs | Id | NO ACTION |
| FK_student_profiles_departments_DepartmentId | student_profiles | DepartmentId | departments | Id | NO ACTION |
| FK_course_offerings_courses_CourseId | course_offerings | CourseId | courses | Id | NO ACTION |
| FK_course_offerings_semesters_SemesterId | course_offerings | SemesterId | semesters | Id | NO ACTION |
| FK_enrollments_course_offerings_CourseOfferingId | enrollments | CourseOfferingId | course_offerings | Id | NO ACTION |
| FK_enrollments_student_profiles_StudentProfileId | enrollments | StudentProfileId | student_profiles | Id | NO ACTION |
| FK_assignment_submissions_assignments_AssignmentId | assignment_submissions | AssignmentId | assignments | Id | NO ACTION |
| FK_notification_recipients_notifications_NotificationId | notification_recipients | NotificationId | notifications | Id | NO ACTION |
| FK_fyp_meetings_fyp_projects_FypProjectId | fyp_meetings | FypProjectId | fyp_projects | Id | NO ACTION |
| FK_fyp_panel_members_fyp_projects_FypProjectId | fyp_panel_members | FypProjectId | fyp_projects | Id | NO ACTION |
| FK_quiz_attempts_quizzes_QuizId | quiz_attempts | QuizId | quizzes | Id | NO ACTION |
| FK_quiz_questions_quizzes_QuizId | quiz_questions | QuizId | quizzes | Id | NO ACTION |
| FK_quiz_answers_quiz_attempts_QuizAttemptId | quiz_answers | QuizAttemptId | quiz_attempts | Id | NO ACTION |
| FK_quiz_answers_quiz_questions_QuizQuestionId | quiz_answers | QuizQuestionId | quiz_questions | Id | NO ACTION |
| FK_quiz_options_quiz_questions_QuizQuestionId | quiz_options | QuizQuestionId | quiz_questions | Id | NO ACTION |
| FK_chat_messages_chat_conversations_ConversationId | chat_messages | ConversationId | chat_conversations | Id | NO ACTION |
| fk_acr_requestor_user | admin_change_requests | RequestorUserId | users | Id | NO ACTION |
| fk_acr_reviewer_user | admin_change_requests | ReviewedByUserId | users | Id | NO ACTION |
| fk_pr_confirmed_by_user | payment_receipts | ConfirmedByUserId | users | Id | NO ACTION |
| fk_pr_created_by_user | payment_receipts | CreatedByUserId | users | Id | NO ACTION |
| fk_pr_student_profile | payment_receipts | StudentProfileId | student_profiles | Id | NO ACTION |
| fk_tmr_reviewer_user | teacher_modification_requests | ReviewedByUserId | users | Id | NO ACTION |
| fk_tmr_teacher_user | teacher_modification_requests | TeacherUserId | users | Id | NO ACTION |
| FK_module_role_assignments_modules_ModuleId | module_role_assignments | ModuleId | modules | Id | NO ACTION |
| FK_timetables_departments_DepartmentId | timetables | DepartmentId | departments | Id | NO ACTION |
| FK_timetables_semesters_SemesterId | timetables | SemesterId | semesters | Id | NO ACTION |
| FK_report_role_assignments_report_definitions_ReportDefinitionId | report_role_assignments | ReportDefinitionId | report_definitions | Id | NO ACTION |
| FK_timetable_entries_timetables_TimetableId | timetable_entries | TimetableId | timetables | Id | NO ACTION |
| FK_rooms_buildings_BuildingId | rooms | BuildingId | buildings | Id | NO ACTION |
| FK_timetable_entries_buildings_BuildingId | timetable_entries | BuildingId | buildings | Id | NO ACTION |
| FK_timetable_entries_courses_CourseId | timetable_entries | CourseId | courses | Id | NO ACTION |
| FK_timetable_entries_rooms_RoomId | timetable_entries | RoomId | rooms | Id | NO ACTION |
| FK_timetable_entries_users_FacultyUserId | timetable_entries | FacultyUserId | users | Id | NO ACTION |
| FK_timetables_academic_programs_AcademicProgramId | timetables | AcademicProgramId | academic_programs | Id | NO ACTION |
| FK_sidebar_menu_items_sidebar_menu_items_ParentId | sidebar_menu_items | ParentId | sidebar_menu_items | Id | NO ACTION |
| FK_sidebar_menu_role_accesses_sidebar_menu_items_SidebarMenuItemId | sidebar_menu_role_accesses | SidebarMenuItemId | sidebar_menu_items | Id | NO ACTION |
| FK_admin_department_assignments_departments_DepartmentId | admin_department_assignments | DepartmentId | departments | Id | NO ACTION |
| FK_academic_deadlines_semesters_SemesterId | academic_deadlines | SemesterId | semesters | Id | NO ACTION |
| FK_support_ticket_messages_support_tickets_TicketId | support_ticket_messages | TicketId | support_tickets | Id | NO ACTION |
| FK_course_prerequisites_courses_CourseId | course_prerequisites | CourseId | courses | Id | NO ACTION |
| FK_course_prerequisites_courses_PrerequisiteCourseId | course_prerequisites | PrerequisiteCourseId | courses | Id | NO ACTION |
| FK_rubric_criteria_rubrics_RubricId | rubric_criteria | RubricId | rubrics | Id | NO ACTION |
| FK_rubric_levels_rubric_criteria_RubricCriterionId | rubric_levels | RubricCriterionId | rubric_criteria | Id | NO ACTION |
| FK_degree_rules_academic_programs_AcademicProgramId | degree_rules | AcademicProgramId | academic_programs | Id | NO ACTION |
| FK_degree_rule_required_courses_courses_CourseId | degree_rule_required_courses | CourseId | courses | Id | NO ACTION |
| FK_degree_rule_required_courses_degree_rules_DegreeRuleId | degree_rule_required_courses | DegreeRuleId | degree_rules | Id | NO ACTION |
| FK_graduation_applications_student_profiles_StudentProfileId | graduation_applications | StudentProfileId | student_profiles | Id | NO ACTION |
| FK_graduation_application_approvals_graduation_applications_GraduationApplicationId | graduation_application_approvals | GraduationApplicationId | graduation_applications | Id | NO ACTION |
| FK_course_grading_configs_courses_CourseId | course_grading_configs | CourseId | courses | Id | NO ACTION |
| FK_course_announcements_course_offerings_OfferingId | course_announcements | OfferingId | course_offerings | Id | NO ACTION |
| FK_course_content_modules_course_offerings_OfferingId | course_content_modules | OfferingId | course_offerings | Id | NO ACTION |
| FK_discussion_threads_course_offerings_OfferingId | discussion_threads | OfferingId | course_offerings | Id | NO ACTION |
| FK_content_videos_course_content_modules_ModuleId | content_videos | ModuleId | course_content_modules | Id | NO ACTION |
| FK_discussion_replies_discussion_threads_ThreadId | discussion_replies | ThreadId | discussion_threads | Id | NO ACTION |
| FK_study_plans_student_profiles_StudentProfileId | study_plans | StudentProfileId | student_profiles | Id | NO ACTION |
| FK_study_plan_courses_courses_CourseId | study_plan_courses | CourseId | courses | Id | NO ACTION |
| FK_study_plan_courses_study_plans_StudyPlanId | study_plan_courses | StudyPlanId | study_plans | Id | NO ACTION |
| FK_student_stream_assignments_school_streams_SchoolStreamId | student_stream_assignments | SchoolStreamId | school_streams | Id | NO ACTION |
| FK_login_activity_logs_users_UserId | login_activity_logs | UserId | users | Id | NO ACTION |
| FK_incident_logs_users_ReportedBy | incident_logs | ReportedBy | users | Id | NO ACTION |
| FK_incident_logs_users_AssignedTo | incident_logs | AssignedTo | users | Id | NO ACTION |
| FK_policy_document_versions_policy_documents_DocumentId | policy_document_versions | DocumentId | policy_documents | Id | NO ACTION |
| FK_backup_verification_logs_backup_logs_BackupLogId | backup_verification_logs | BackupLogId | backup_logs | Id | NO ACTION |

**Cascade delete rules:** Only `module_status` (CASCADE on ModuleId) and `user_sessions` (CASCADE on UserId). All others are NO ACTION.

---

## B. Indexes (209)

### B.1 Unique Indexes
| Table | Index | Columns | Filter |
|---|---|---|---|
| departments | IX_departments_code | [Code] | |
| module_status | IX_module_status_module_id | [ModuleId] | |
| modules | IX_modules_key | [Key] | |
| roles | IX_roles_name | [Name] | |
| user_sessions | IX_user_sessions_token_hash | [RefreshTokenHash] | |
| users | IX_users_email | [Email] | WHERE [email] IS NOT NULL |
| users | IX_users_username | [Username] | |
| discussion_threads | IX_discussion_threads_TicketNumber | [TicketNumber] | |
| tenants | IX_tenants_code | [Code] | |
| campuses | IX_campuses_tenant_code | [TenantId], [Code] | |
| departments | IX_departments_tenant_campus_code | [TenantId], [CampusId], [Code] | |
| academic_programs | IX_academic_programs_code_department | [Code], [DepartmentId] | |
| academic_programs | IX_academic_programs_code | [Code] | |
| course_offerings | IX_course_offerings_course_semester | [CourseId], [SemesterId] | |
| courses | IX_courses_code_department | [Code], [DepartmentId] | |
| enrollments | IX_enrollments_student_offering | [StudentProfileId], [CourseOfferingId] | |
| registration_whitelist | — | [IdentifierValue] | |
| student_profiles | IX_student_profiles_registration_number | [RegistrationNumber] | |
| student_profiles | IX_student_profiles_user_id | [UserId] | |
| assignment_submissions | IX_assignment_submissions_unique | [AssignmentId], [StudentProfileId] | |
| results | IX_results_student_offering_type | [StudentProfileId], [CourseOfferingId], [ResultType] | (×2) |
| attendance_records | IX_attendance_unique | [StudentProfileId], [CourseOfferingId], [Date] | |
| notification_recipients | IX_notification_recipients_unique | [NotificationId], [RecipientUserId] | |
| fyp_panel_members | IX_fyp_panel_members_unique | [FypProjectId], [UserId], [Role] | |
| quiz_answers | IX_quiz_answers_unique | [QuizAttemptId], [QuizQuestionId] | |
| consumed_verification_keys | IX_consumed_verification_keys_keyhash | [KeyHash] | |
| payment_receipts | IX_payment_receipts_receipt_no | [ReceiptNo] | (×2) |
| module_role_assignments | IX_module_role_assignments_unique | [ModuleId], [RoleName] | |
| report_definitions | IX_report_definitions_key | [Key] | |
| report_role_assignments | IX_report_role_assignments_unique | [ReportDefinitionId], [RoleName] | |
| buildings | IX_buildings_unique | [TenantId], [CampusId], [Code] | |
| rooms | IX_rooms_unique | [TenantId], [CampusId], [BuildingId], [Number] | WHERE TenantId IS NOT NULL AND CampusId IS NOT NULL |
| sidebar_menu_items | IX_sidebar_menu_items_key | [Key] | |
| sidebar_menu_role_accesses | IX_sidebar_menu_role_accesses_unique | [SidebarMenuItemId], [RoleName] | |
| gpa_scale_rules | IX_gpa_scale_rules_unique | [InstitutionType], [MinimumScore] | (×2) |
| result_component_rules | IX_result_component_rules_unique | [InstitutionType], [Name] | (×2) |
| portal_settings | IX_portal_settings_key | [Key] | |
| course_prerequisites | IX_course_prerequisites_unique | [CourseId], [PrerequisiteCourseId] | |
| rubric_student_grades | IX_rubric_student_grades_unique | [AssignmentSubmissionId], [RubricCriterionId] | |
| degree_rules | IX_degree_rules_program | [AcademicProgramId] | |
| degree_rule_required_courses | IX_degree_rule_required_courses_unique | [DegreeRuleId], [CourseId] | |
| course_grading_configs | IX_course_grading_configs_course | [CourseId] | |
| study_plan_courses | IX_study_plan_courses_unique | [StudyPlanId], [CourseId] | |
| institution_grading_profiles | IX_institution_grading_profiles_inst | [InstitutionType] | |
| bulk_promotion_entries | IX_bulk_promotion_entries_unique | [BatchId], [StudentProfileId] | |
| parent_student_links | IX_parent_student_links_unique | [ParentUserId], [StudentProfileId] | |
| school_streams | IX_school_streams_name | [Name] | |
| student_stream_assignments | IX_student_stream_assignments_unique | [StudentProfileId] | |
| accreditation_templates | — | [Name] | |
| policy_document_versions | IX_policy_doc_versions_doc_version | [DocumentId], [VersionNumber] | |
| role_resource_permissions | IX_role_resource_permissions_unique | [RoleName], [ResourceKey] | |

### B.2 Non-Unique Indexes (performance lookups)
All remaining ~130 indexes cover common query patterns: lookup by FK, range scans on dates, composite status/date indexes for filtering. Key coverage includes:

- **audit_logs**: actor, occurred_at, entity_name, correlation_id, event_category, severity, actor_role
- **users**: role_id, tenant_id, campus_id, composite tenant+campus+active+role, is_locked_out filtered
- **departments**: tenant, campus, institution_type, name
- **student_profiles**: program, department, status
- **enrollments**: course_offering, student_profile, status
- **course_offerings**: semester+is_open, faculty+is_open, tenant+campus+institution+is_open
- **attendance_records**: course_offering+date, student_profile
- **results**: course_offering, student_profile (multiple variants)
- **quizzes/quiz_attempts/quiz_questions/quiz_options**: all FK paths + composite lookups
- **assignments**: course_offering, course_offering+is_published
- **fyp_projects**: department+status, student_profile, supervisor
- **payment_receipts**: student_profile, status, due_date, confirmed_by, created_by, status+due_date
- **timetables/timetable_entries**: department+semester, academic_program, FK lookups
- **notification_recipients**: recipient+is_read, recipient+created_at
- **support_tickets**: submitter, department, status, assigned_to
- **bulk_promotion_batches**: status+created_at
- **graduation_applications**: student_profile, status+created_at
- **login_activity_logs**: user_id, attempted_at, is_success+attempted_at, ip+attempted_at
- **backup_logs**: status+started_at, backup_type+started_at
- **data_classification_entries**: entity_name+entity_id, classification_level+classified_at
- **incident_logs**: status+reported_at, severity+status, reported_by
- **policy_documents**: status, category
- **backup_verification_logs**: backup_log+verified_at, is_successful+verified_at
- **user_sessions / password_history**: user_id+created_at (DESC)

---

## C. Check Constraints (2)

| Constraint | Table | Expression |
|---|---|---|
| CK_users_tenant_campus_pair | users | `([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL)` |
| CK_departments_tenant_campus_pair | departments | `([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL)` |

Both enforce that TenantId and CampusId are always set together or both NULL — never one without the other.

---

## D. Default Constraints

All default constraints follow naming convention `DF_<table>_<column>` (SQL Server auto-generated with EF Core). Key defaults:

| Table | Column | Default |
|---|---|---|
| tenants | IsActive | CAST(1 AS bit) |
| tenants | IsDeleted | CAST(0 AS bit) |
| campuses | IsActive | CAST(1 AS bit) |
| campuses | IsDeleted | CAST(0 AS bit) |
| departments | IsDeleted | CAST(0 AS bit) |
| departments | InstitutionType | 0 |
| buildings | IsActive | CAST(1 AS bit) |
| buildings | IsDeleted | CAST(0 AS bit) |
| rooms | IsActive | CAST(1 AS bit) |
| rooms | IsDeleted | CAST(0 AS bit) |
| users | IsDeleted | CAST(0 AS bit) |
| discussion_threads | ThreadType | N'Issue' |
| discussion_threads | IsSolved | CAST(0 AS bit) |
| discussion_threads | IsVisibleToAll | CAST(0 AS bit) |
| course_offerings | IsDeleted | CAST(0 AS bit) |
| timetables | IsDeleted | CAST(0 AS bit) |
| timetables | AcademicProgramId | '00000000-0000-0000-0000-000000000000' |
| timetables | EffectiveDate | '0001-01-01' |
| timetables | SemesterNumber | 0 |
| academic_programs | IsDeleted | CAST(0 AS bit) |
| courses | IsDeleted | CAST(0 AS bit) |
| assignments | IsDeleted | CAST(0 AS bit) |
| gpa_scale_rules | InstitutionType | 0 |
| result_component_rules | InstitutionType | 0 |
| student_profiles | IsDeleted | CAST(0 AS bit) |
| bulk_promotion_entries | IsApplied | CAST(0 AS bit) |
| modules | IsDeleted | CAST(0 AS bit) | (where applicable)

---

## E. Nullability Summary

All columns in the schema follow these rules:
- **PK columns** (`Id`): always `NOT NULL`
- **FK columns**: mostly `NOT NULL` except optional relationships (e.g., `users.DepartmentId` is NULL for SuperAdmin, `users.TenantId`/`CampusId` are NULL for SuperAdmin)
- **`CreatedAt`**: always `NOT NULL`
- **`UpdatedAt`**: always `NULL` (set on update)
- **`RowVersion`**: always `NULL` (auto-managed by SQL Server)
- **`IsDeleted`**: always `NOT NULL` with default `0`
- **`DeletedAt`**: always `NULL`
- **Description/text columns**: typically `NULL`

For precise per-column nullability, see the type column in the table definitions above (`NOT NULL` vs `NULL` suffix).

---

## F. Identity Columns

| Table | Column | Type |
|---|---|---|
| roles | Id | int IDENTITY |
| audit_logs | Id | bigint IDENTITY |

All other tables use `uniqueidentifier` (GUID) as PK with `NEWID()` / `NEWSEQUENTIALID()` generation at application level.

---

## G. RowVersion Columns

Every table (except metadata/config tables) has `RowVersion rowversion NULL` for optimistic concurrency control. Tables without rowversion: `__EFMigrationsHistory`, `chat_conversations`, `chat_messages`, `outbound_email_logs`, `password_history`, `consumed_verification_keys`, `backup_logs`, `data_classification_entries`, `incident_logs`, `policy_documents`, `policy_document_versions`, `backup_verification_logs`.

---

## H. Cascade Delete Rules

Only 2 foreign keys use CASCADE delete:
- `FK_module_status_modules_ModuleId`: DELETE from `modules` cascades to `module_status`
- `FK_user_sessions_users_UserId`: DELETE from `users` cascades to `user_sessions`

All other 71 foreign keys use `ON DELETE NO ACTION`.

---

## I. Views

Two views defined in `04-Maintenance-Indexes-And-Views.sql`:

- **`vw_StudentAttendanceSummary`**: Aggregates attendance by student + semester with percentage calculation
- **`vw_StudentResultsSummary`**: Aggregates results by student + semester with average marks

No schema-level views, triggers, functions, or stored procedures are defined in `01-Schema-Current.sql`. All business logic is in the application layer (C#).

---

## J. Collation

All `nvarchar`/`nvarchar(max)` columns use the database default collation. No column-level collation overrides are specified. The database default is SQL Server instance default (typically `SQL_Latin1_General_CP1_CI_AS`).

---

## K. Computed Columns

None. The schema uses no computed or persisted computed columns.

---

*Schema version: 1.0 | Tables: 91 | FKs: 73 | Indexes: 209 | CHECKs: 2 | Generated 2026-06-09*

## MFA / Two-Factor Authentication Columns (2026-06-18)

**User table** (users) � RFC 6238 TOTP via Otp.NET 1.4.1:
- MfaIsEnabled BIT NOT NULL DEFAULT 0 � Whether MFA is active for this user
- MfaTotpSecret NVARCHAR(200) NULL � Base32-encoded TOTP secret key
- MfaRecoveryCodesHashJson NVARCHAR(MAX) NULL � JSON array of SHA-256 hashed recovery codes
- LastPasswordChangedAt DATETIME2 NULL � Password ageing policy (ISO 27001 A.9.4.3)
- ConsentToMonitoring BIT NULL � GDPR monitoring consent (ISO 27001 A.18.1.4)
- DataRetentionDate DATETIME2 NULL � Data lifecycle management

**Supporting entities:**
- login_activity_logs � Immutable login attempt records (success/failure/risk) � ISO 27001 A.12.4.1
- udit_logs � Enhanced with CorrelationId, Severity, EventCategory � ISO 27001 A.12.4.1

