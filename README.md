# RoadAware

RoadAware is a modern web application designed to help monitor and manage road conditions, with a particular focus on pothole detection and road maintenance prioritization. The application combines a React frontend with an ASP.NET Core backend to provide a seamless user experience.

## ?? Features

- **Modern Tech Stack**: Built with React + Vite frontend and ASP.NET Core 8 backend
- **Real-time Processing**: Handles image processing and analysis for road condition assessment
- **AI Integration**: Utilizes OpenAI for intelligent road condition analysis
- **RESTful API**: Comprehensive API endpoints for road condition data management
- **Responsive Design**: Mobile-friendly interface for field usage

## ?? Technology Stack

### Frontend
- React
- Vite
- ESLint for code quality
- HMR (Hot Module Replacement) for development

### Backend
- ASP.NET Core 8
- Web API
- OpenAI integration for intelligent analysis
- File handling and processing capabilities

## ?? Project Structure

```
RoadAware/
??? roadaware.Client/          # React frontend application
?   ??? src/                  # Source files
?   ??? public/               # Static assets
??? roadaware.Server/         # ASP.NET Core backend
    ??? Controllers/          # API endpoints
    ??? Services/             # Business logic and services
```

## ?? Getting Started

### Prerequisites
- .NET 8.0 SDK
- Node.js and npm
- Visual Studio 2022 or VS Code

### Development Setup
1. Clone the repository
2. Backend Setup:
   ```bash
   cd roadaware.Server
   dotnet restore
   dotnet build
   ```
3. Frontend Setup:
   ```bash
   cd roadaware.client
   npm install
   ```

### Running the Application
- The application can be run using Visual Studio or through the command line
- Both frontend and backend are configured to work together through proxy settings

## ?? Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## ?? License

[Add your license information here]

## ?? Additional Resources

- [React Documentation](https://react.dev)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core)
- [Vite Documentation](https://vitejs.dev)