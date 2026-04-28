# Placement Management System (PMS)

A comprehensive Placement Management System built with ASP.NET Core MVC to streamline the campus recruitment process. The system connects administrators (placement officers) and students, facilitating job postings, student registrations, application tracking, and data management.

## Features

- **Role-Based Access Control**: Separate interfaces and functionalities for Administrators and Students.
- **Job Management**: Admins can create, edit, and manage job postings, specifying branch eligibility, package details, and other requirements.
- **Student Profiles**: Students can register, build their profiles, and apply for eligible job opportunities based on their branch and academic criteria.
- **Application Tracking**: Seamless tracking of student applications for specific job roles.
- **Data Export**: Admins can export student and application data to Excel (via ClosedXML) for reporting and analysis.
- **Real-Time Features**: Integrated with SignalR for instant updates and dynamic interactions.
- **Containerization**: Docker support for easy deployment and consistency across different environments.

## Tech Stack

- **Framework**: .NET 10 (ASP.NET Core MVC)
- **Database**: SQLite (managed via Entity Framework Core)
- **Real-Time Communication**: SignalR
- **Authentication**: ASP.NET Core Authentication
- **Reporting/Export**: ClosedXML
- **Deployment**: Docker & Docker Compose

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional, if you prefer running via containers)

### Running Locally

1. Clone the repository:
   ```bash
   git clone https://github.com/Neev-Chovatiya/placement-management-system.git
   cd placement-management-system
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Update the database (Apply migrations):
   ```bash
   dotnet ef database update
   ```

4. Run the application:
   ```bash
   dotnet run
   ```
   The application will be accessible at the URL specified in your launch settings (typically `https://localhost:5001` or `http://localhost:5000`).

### Running with Docker

1. Build and run the application using Docker Compose:
   ```bash
   docker-compose up --build
   ```

2. Once the containers are running, the application will be accessible via your browser based on the ports mapped in `docker-compose.yml`.

## Contributing

Contributions, issues, and feature requests are welcome!
