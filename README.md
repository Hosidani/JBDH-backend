# JBDH 

JBDH is a job search and job applications management web application built with ASP.NET Core MVC and Entity Framework Core. It allows users to search for jobs, view job details, apply or unapply for jobs, and receive AI-powered job analysis as well as CV analysis.

## Features

- **Job Search:** Search for jobs by description and location.
- **Job Details:** View detailed information about each job.
- **Apply/Unapply:** Apply for jobs with optional notes, or withdraw applications.
- **AI Analysis:** Get AI-generated insights for job postings.
- **User Authentication:** Secure login and registration.
- **Admin & Recruiter Tools:** (If applicable) Manage job postings and users.

## Technologies

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server/MySQL (configurable)
- C#
- Bootstrap (for UI)
- Markdig (for Markdown rendering)
- AI Integration (custom service)

## Getting Started

### Prerequisites

- [.NET 6 SDK or later](https://dotnet.microsoft.com/download)
- SQL Server or MySQL (update `appsettings.json` with your connection string)

### Setup

1. **Clone the repository:**
    ```sh
    git clone https://github.com/yourusername/JBDH.git
    cd JBDH
    ```

2. **Configure the database:**
    - Update `appsettings.json` with your database connection string.

3. **Apply migrations:**
    ```sh
    dotnet ef database update
    ```

4. **Run the application:**
    ```sh
    dotnet run
    ```

5. **Open in your browser:**
    ```
    http://localhost:5071/
    ```

## Project Structure

- `Controllers/` - MVC controllers for jobs, applications, search, etc.
- `Models/` - Entity and view models.
- `Views/` - Razor views for UI.
- `Services/` - Business logic and integration services.
- `wwwroot/` - Static files (CSS, JS, images).

## Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you would like to change. 

## License

[MIT](LICENSE)

---

**Contact:**  
For questions or support, please open an issue or contact the maintainer. 