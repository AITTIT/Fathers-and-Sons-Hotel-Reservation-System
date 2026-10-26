# Copilot Instructions

## Project Guidelines
- For this project, use the MVVM pattern in the MAUI client architecture, with Models, XAML Views, ViewModels, and service abstractions separated by feature.
- Use a split architecture: MAUI client -> ASP.NET Core Web API -> SQLite persistence.
- Keep persistence and backend business logic in the API project; MAUI ViewModels should use API-facing service abstractions and shared contract DTOs.
- Defer Docker and Docker Compose implementation until later assignments; proceed first with backend API endpoint development and tests without database dependency.
