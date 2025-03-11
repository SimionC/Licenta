-- Users table
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT UNIQUE NOT NULL,
    Nume TEXT NOT NULL,
    Prenume TEXT NOT NULL,
    Password TEXT NOT NULL, -- Store hashed passwords!
    StudentId TEXT UNIQUE, -- Nullable for teachers/admins
    UserTypeId INTEGER NOT NULL,
    FOREIGN KEY (UserTypeId) REFERENCES UserTypes(Id)
);

-- UserTypes table with JSON-based permissions
CREATE TABLE UserTypes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Permissions TEXT NOT NULL -- Stores JSON as a string
);

-- Users-Courses (many-to-many relationship)
CREATE TABLE Users_Courses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    CourseId INTEGER NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (CourseId) REFERENCES Courses(Id)
);

-- Courses table
CREATE TABLE Courses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Description TEXT
);

-- CourseWork table (Assignments)
CREATE TABLE CourseWork (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CourseId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id)
);

-- Notes table
CREATE TABLE Notes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Guid TEXT UNIQUE NOT NULL, -- Randomly generated identifier
    Text TEXT NOT NULL,
    CreationDate TEXT DEFAULT CURRENT_TIMESTAMP,
    ModifyDate TEXT DEFAULT CURRENT_TIMESTAMP,
    UserId INTEGER NOT NULL,
    VisibilityTypeId INTEGER NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (VisibilityTypeId) REFERENCES VisibilityType(Id)
);

-- Users-Notes (for shared notes with permissions)
CREATE TABLE Users_Notes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    NoteId INTEGER NOT NULL,
    PermissionLevel TEXT CHECK (PermissionLevel IN ('view', 'edit')),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (NoteId) REFERENCES Notes(Id)
);

-- Courses-Notes (linking notes to courses)
CREATE TABLE Courses_Notes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NoteId INTEGER NOT NULL,
    CourseId INTEGER NOT NULL,
    FOREIGN KEY (NoteId) REFERENCES Notes(Id),
    FOREIGN KEY (CourseId) REFERENCES Courses(Id)
);

-- VisibilityType table (defines note visibility)
CREATE TABLE VisibilityType (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE -- public, hidden, private
);

-- SubmittedWork table (students submitting assignments)
CREATE TABLE SubmittedWork (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NoteId INTEGER NOT NULL, -- The note (submission)
    GradeId INTEGER,
    FOREIGN KEY (NoteId) REFERENCES Notes(Id),
    FOREIGN KEY (GradeId) REFERENCES Grades(Id)
);

-- Grades table
CREATE TABLE Grades (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Description TEXT,
    Grade INTEGER CHECK (Grade BETWEEN 1 AND 10) NOT NULL
);

-- Let's insert some data 
-- Firstly the JSON
INSERT INTO UserTypes (Name, Permissions) VALUES 
('Student', '{"view_notes": true, "edit_notes": true, "create_courses": false, "edit_courses": false, "grade_students": false, "manage_users": false}'),
('Teacher', '{"view_notes": true, "edit_notes": true, "create_courses": true, "edit_courses": true, "grade_students": true, "manage_users": false}'),
('Admin', '{"view_notes": true, "edit_notes": true, "create_courses": true, "edit_courses": true, "grade_students": true, "manage_users": true}');

INSERT INTO Users (Email, Nume, Prenume, Password, StudentId, UserTypeId) VALUES
('john.doe@example.com', 'Doe', 'John', 'hashed_password_here', 'S123456', 1);

INSERT INTO Courses (Title, Description) VALUES 
('Introduction to Databases', 'Learn the basics of SQL and database management.');

INSERT INTO Users_Courses (UserId, CourseId) VALUES 
(1, 1); -- Assuming UserId = 1 and CourseId = 1


SELECT * FROM Users;
SELECT * FROM Courses;
SELECT * FROM Users_Courses;

