# TaskIA

API construída com **Clean Architecture** e princípios **SOLID**.

## 📁 Estrutura do Projeto

```
TaskIA/
│
├── Domain/                      # Camada de Domínio
│   ├── Common/                  # Classes compartilhadas (Result pattern)
│   ├── Entities/                # Entidades de domínio
│   └── Interfaces/              # Interfaces (repositórios, contratos)
│
├── Application.Core/            # Camada de Aplicação
│   ├── UseCases/                # Casos de uso / Services
│   ├── DTOs/                    # Data Transfer Objects
│   ├── Validators/              # Validadores FluentValidation
│   └── Interfaces/              # Interfaces de Use Cases
│
├── API/                         # Camada de Apresentação (Web API)
│   ├── Controllers/             # Endpoints REST
│   └── Program.cs               # Configuração da aplicação
│
├── Infrastructure/              # Camada de Infraestrutura
│   ├── Data/                    # DbContext
│   ├── Repositories/            # Implementações de repositórios
│   └── UnitOfWork/              # Pattern Unit of Work
│
└── CrossCutting/                # Concerns Transversais
    ├── Exceptions/              # Exceções customizadas
    ├── Middlewares/             # Middlewares globais
    └── Extensions/              # Extension methods
```

## 🏗️ Arquitetura

### Princípios Aplicados

- **SOLID**
  - **S**ingle Responsibility: Cada classe tem uma única responsabilidade
  - **O**pen/Closed: Aberto para extensão, fechado para modificação
  - **L**iskov Substitution: Interfaces bem definidas e substituíveis
  - **I**nterface Segregation: Interfaces específicas por necessidade
  - **D**ependency Inversion: Dependência de abstrações, não implementações

- **Clean Code**
  - Nomenclatura clara e significativa
  - Funções pequenas e focadas
  - Comentários onde necessário
  - DRY (Don't Repeat Yourself)

- **Clean Architecture**
  - Separação de responsabilidades por camadas
  - Dependências apontam para o centro (Domain)
  - Regras de negócio independentes de frameworks

### Camadas

#### 1️⃣ Domain
Núcleo da aplicação, sem dependências externas. Contém:
- Entidades de negócio
- Interfaces de repositórios
- Lógica de domínio pura

#### 2️⃣ Application.Core
Lógica de aplicação. Contém:
- Use Cases (casos de uso)
- DTOs para entrada/saída
- Validadores
- Interfaces de serviços

#### 3️⃣ API
Camada de apresentação (Controllers). Contém:
- Controllers REST
- Configuração de rotas
- Middleware pipeline
- Swagger/OpenAPI

#### 4️⃣ Infrastructure
Implementações técnicas. Contém:
- Acesso a dados (EF Core)
- Repositórios concretos
- Configurações de banco

#### 5️⃣ CrossCutting
Funcionalidades transversais. Contém:
- Tratamento global de exceções
- Logging
- Middlewares
- Extensions

## 🚀 Como Começar

### Pré-requisitos

- .NET 9.0 SDK
- SQL Server / PostgreSQL / ou use InMemory (configurado por padrão)
- IDE: Visual Studio, VS Code ou Rider

### Instalação

1. Clone o repositório
```bash
git clone <url-do-repositorio>
cd TaskIA
```

2. Restaure os pacotes
```bash
dotnet restore
```

3. Configure a connection string em `appsettings.json` (se usar banco real)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "sua-connection-string"
  }
}
```

4. Execute as migrations (quando criar entidades)
```bash
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project API
dotnet ef database update --project Infrastructure --startup-project API
```

5. Execute a aplicação
```bash
dotnet run --project API
```

6. Acesse o Swagger
```
https://localhost:5001
```

## 📝 Como Adicionar Novas Funcionalidades

### 1. Criar uma Entidade

```csharp
// Domain/Entities/Task.cs
public class Task : BaseEntity
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool IsCompleted { get; private set; }

    private Task() { } // EF Core

    public Task(string title, string description)
    {
        Title = title;
        Description = description;
        IsCompleted = false;
    }

    public void Complete() => IsCompleted = true;
}
```

### 2. Criar um DTO

```csharp
// Application.Core/DTOs/TaskDto.cs
namespace Application.Core.DTOs;

public record TaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
}
```

### 3. Criar um Use Case

```csharp
// Application.Core/UseCases/Tasks/CreateTaskUseCase.cs
using Application.Core.Interfaces;
using Application.Core.DTOs;
using Domain.Common;
using Domain.Interfaces;

namespace Application.Core.UseCases.Tasks;

public class CreateTaskUseCase : IUseCase<CreateTaskRequest, TaskDto>
{
    private readonly IRepository<Task> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaskUseCase(IRepository<Task> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TaskDto>> ExecuteAsync(
        CreateTaskRequest request, 
        CancellationToken cancellationToken)
    {
        var task = new Task(request.Title, request.Description);
        await _repository.AddAsync(task, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var dto = new TaskDto 
        { 
            Id = task.Id, 
            Title = task.Title, 
            Description = task.Description 
        };

        return Result<TaskDto>.Success(dto, "Task criada com sucesso");
    }
}

public record CreateTaskRequest(string Title, string Description);
```

### 4. Criar um Validator

```csharp
// Application.Core/Validators/CreateTaskRequestValidator.cs
using FluentValidation;

namespace Application.Core.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título é obrigatório")
            .MaximumLength(200).WithMessage("Título deve ter no máximo 200 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descrição deve ter no máximo 1000 caracteres");
    }
}
```

### 5. Registrar no DI

```csharp
// Application.Core/DependencyInjection.cs
services.AddScoped<IUseCase<CreateTaskRequest, TaskDto>, CreateTaskUseCase>();
```

### 6. Criar o Controller

```csharp
// API/Controllers/TasksController.cs
using Application.Core.Interfaces;
using Application.Core.UseCases.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request,
        [FromServices] IUseCase<CreateTaskRequest, TaskDto> useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

## 🔧 Configuração de Banco de Dados

Por padrão, o projeto usa **InMemory** para facilitar o desenvolvimento. Para usar um banco real:

### SQL Server
```csharp
// Infrastructure/DependencyInjection.cs
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```

### PostgreSQL
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
```

## 📚 Padrões Utilizados

- **Repository Pattern**: Abstração do acesso a dados
- **Unit of Work**: Gerenciamento de transações
- **Result Pattern**: Retorno de operações sem exceções
- **Dependency Injection**: Inversão de controle
- **DTO Pattern**: Separação entre domínio e apresentação

## 🧪 Testes (A Implementar)

Crie projetos de teste para cada camada:
```bash
dotnet new xunit -n Domain.Tests
dotnet new xunit -n Application.Tests
dotnet new xunit -n Infrastructure.Tests
```

## 📦 Pacotes Principais

- **Microsoft.EntityFrameworkCore** - ORM
- **FluentValidation** - Validação de dados
- **Swashbuckle.AspNetCore** - Documentação OpenAPI/Swagger

## 👥 Contribuindo

1. Siga os princípios SOLID e Clean Code
2. Mantenha a separação de responsabilidades entre camadas
3. Documente código complexo
4. Escreva testes unitários
5. Use nomes descritivos para classes, métodos e variáveis

## 📄 Licença

[Defina sua licença aqui]
