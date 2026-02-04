# TaskIA API

API construída com **Clean Architecture** e princípios **SOLID** usando .NET 9.0.

## 📁 Estrutura do Projeto

```
TaskIA-API/
│
├── Domain/                      # ⭐ Camada de Domínio (núcleo)
│   ├── Common/                  # Result Pattern
│   ├── Entities/                # Entidades com Data Annotations
│   │   ├── BaseEntity.cs        # Entidade base (Id, CreatedAt, UpdatedAt)
│   │   └── User.cs              # Exemplo de entidade
│   └── Interfaces/              # Contratos de repositórios
│       ├── IRepository.cs       # Interface genérica
│       ├── IUserRepository.cs   # Interface específica
│       └── IUnitOfWork.cs       # Gerenciamento de transações
│
├── Application.Core/            # 🎯 Camada de Aplicação
│   ├── Services/                # Lógica de negócio
│   │   └── UserService.cs       # EXEMPLO COMPLETO implementado
│   ├── DTOs/                    # Data Transfer Objects
│   │   └── Users/               # DTOs de User
│   ├── Validators/              # FluentValidation
│   │   └── Users/               # Validadores de User
│   └── Interfaces/              # Interfaces de Services
│       └── IUserService.cs
│
├── Application/                 # 🌐 Camada de Apresentação (API)
│   ├── Controllers/             # Endpoints REST
│   │   ├── HealthController.cs
│   │   └── UsersController.cs
│   ├── Program.cs               # Configuração da aplicação
│   └── appsettings.json         # Configurações
│
├── Infrastructure/              # 🔧 Camada de Infraestrutura
│   ├── Data/                    
│   │   ├── ApplicationDbContext.cs  # EF Core DbContext
│   │   └── Migrations/          # Migrations do banco
│   ├── Repositories/            
│   │   ├── Repository.cs        # ✅ Implementação genérica COMPLETA
│   │   └── UserRepository.cs    # ✅ Exemplo específico COMPLETO
│   └── UnitOfWork/              
│       └── UnitOfWork.cs        # ✅ Implementação COMPLETA
│
└── CrossCutting/                # 🔀 Concerns Transversais
    └── Extensions/              # Extension methods
```

## 🏗️ Arquitetura - Princípios SOLID

Veja documentação completa em [SOLID_ARCHITECTURE.md](SOLID_ARCHITECTURE.md)

### ✅ Princípios Implementados

#### **S** - Single Responsibility Principle
- Cada classe tem uma única responsabilidade
- `UserService` → Lógica de negócio de usuários
- `UserRepository` → Acesso a dados de usuários
- `UnitOfWork` → Gerenciamento de transações

#### **O** - Open/Closed Principle
- Extensível via herança: `Repository<T>` pode ser herdado
- Fechado para modificação: Use interfaces

#### **L** - Liskov Substitution Principle
- Qualquer `IRepository<T>` pode ser substituído
- `UserRepository` substitui `Repository<User>` perfeitamente

#### **I** - Interface Segregation
- Interfaces específicas: `IUserRepository` para User
- Interfaces genéricas: `IRepository<T>` para todos

#### **D** - Dependency Inversion
- Dependência de abstrações (interfaces)
- Injeção de dependência em todos os lugares

## 🚀 Quick Start

### 1️⃣ Pré-requisitos

- **.NET 9.0 SDK**
- **SQL Server** (ou LocalDB)
- **Visual Studio 2022** / VS Code / Rider

### 2️⃣ Instalação

```bash
# Clone o repositório
git clone <url-do-repositorio>
cd TaskIA-API

# Restaure os pacotes
dotnet restore

# Execute as migrations
dotnet ef database update --project Infrastructure --startup-project Application
```

### 3️⃣ Configuração

Edite `Application/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskIA;Trusted_Connection=true"
  }
}
```

### 4️⃣ Execute

```bash
dotnet run --project Application
```

Acesse: **https://localhost:5001/swagger**

## 📝 Guia para Desenvolvedores

### ✅ Padrão COMPLETO Implementado (Use como Exemplo)

#### 1. UserService - Métodos Implementados

**CreateAsync** - Padrão de Criação
```csharp
✅ Validar regras de negócio
✅ Criar entidade
✅ Adicionar ao repositório
✅ Commit via UnitOfWork
✅ Mapear para DTO
✅ Try/catch com mensagens claras
```

**GetByIdAsync** - Padrão de Busca
```csharp
✅ Buscar no repositório
✅ Validar se encontrou
✅ Mapear para DTO
```

**GetAllAsync** - Padrão de Listagem
```csharp
✅ Buscar todos
✅ Mapear lista com LINQ
✅ Retornar com contagem
```

#### 2. UserRepository - Exemplo Específico

```csharp
public class UserRepository : Repository<User>, IUserRepository
{
    // Métodos específicos de User
    ✅ GetByEmailAsync()
    ✅ EmailExistsAsync()
}
```

#### 3. Repository<T> - Genérico Completo

```csharp
✅ GetByIdAsync()
✅ GetAllAsync()
✅ FindAsync()
✅ AddAsync()
✅ UpdateAsync()
✅ DeleteAsync()
✅ ExistsAsync()
```

### 🆕 Como Adicionar Nova Entidade

#### Passo 1: Criar Entidade

```csharp
// Domain/Entities/Product.cs
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Product : BaseEntity
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }
    
    public Product() { }
}
```

#### Passo 2: Criar Interface do Repositório (Opcional)

```csharp
// Domain/Interfaces/IProductRepository.cs
public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByPriceRangeAsync(
        decimal min, decimal max, CancellationToken cancellationToken = default);
}
```

#### Passo 3: Criar Repositório Específico (Opcional)

```csharp
// Infrastructure/Repositories/ProductRepository.cs
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Product>> GetByPriceRangeAsync(
        decimal min, decimal max, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Price >= min && p.Price <= max)
            .ToListAsync(cancellationToken);
    }
}
```

#### Passo 4: Criar DTOs

```csharp
// Application.Core/DTOs/Products/ProductDto.cs
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
```

#### Passo 5: Criar Service

```csharp
// Application.Core/Services/ProductService.cs
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    // Implemente seguindo o padrão do UserService
}
```

#### Passo 6: Registrar DI

```csharp
// Infrastructure/DependencyInjection.cs
services.AddScoped<IProductRepository, ProductRepository>();

// Application.Core/DependencyInjection.cs
services.AddScoped<IProductService, ProductService>();
```

#### Passo 7: Adicionar DbSet

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public DbSet<Product> Products { get; set; } = null!;
```

#### Passo 8: Criar Migration

```bash
dotnet ef migrations add AddProduct --project Infrastructure --startup-project Application
dotnet ef database update --project Infrastructure --startup-project Application
```

## 🔧 Tecnologias

| Pacote | Versão | Uso |
|--------|--------|-----|
| .NET | 9.0 | Framework |
| Entity Framework Core | 9.0.0 | ORM |
| SQL Server | 9.0.0 | Banco de dados |
| FluentValidation | 12.1.1 | Validação |
| Swashbuckle | 7.2.0 | Swagger/OpenAPI |

## 📦 Padrões Implementados

✅ **Repository Pattern** - Abstração completa de acesso a dados  
✅ **Unit of Work** - Gerenciamento de transações  
✅ **Result Pattern** - Retorno seguro sem exceções  
✅ **Dependency Injection** - Inversão de controle total  
✅ **DTO Pattern** - Separação domínio/apresentação  
✅ **Clean Architecture** - Camadas bem definidas  
✅ **SOLID Principles** - Todos os 5 princípios

## 🎯 Estrutura de Retorno (Result Pattern)

```csharp
// Sucesso
Result<UserDto>.Success(userDto, "Usuário criado com sucesso");

// Falha
Result<UserDto>.Failure("Email já cadastrado");

// No Controller
return result.IsSuccess ? Ok(result) : BadRequest(result);
```

## 🧪 TODO - Testes

```bash
# Criar projetos de teste
dotnet new xunit -n Domain.Tests
dotnet new xunit -n Application.Core.Tests
dotnet new xunit -n Infrastructure.Tests
dotnet new xunit -n API.Tests
```

## 📚 Documentação Adicional

- [SOLID_ARCHITECTURE.md](SOLID_ARCHITECTURE.md) - Princípios SOLID detalhados
- [Swagger UI](https://localhost:5001/swagger) - Documentação interativa da API

## 👥 Convenções de Código

1. ✅ Use **async/await** em todos os métodos de I/O
2. ✅ **CancellationToken** em todos os métodos assíncronos
3. ✅ **Try/catch** em operações de escrita
4. ✅ **Validação** antes de persistir
5. ✅ **Result Pattern** para retornos
6. ✅ **DTOs** para comunicação entre camadas
7. ✅ **Data Annotations** nas entidades
8. ✅ Métodos **privados** para mapeamento (MapToDto)

## 🚀 Próximos Passos

- [ ] Implementar autenticação JWT
- [ ] Adicionar projetos de testes unitários
- [ ] Implementar logging (Serilog)
- [ ] Adicionar Health Checks
- [ ] Implementar paginação
- [ ] Adicionar cache (Redis)
- [ ] Implementar CQRS (opcional)

## 📄 Licença

MIT License## 🔧 Tecnologias

| Pacote | Versão | Uso |
|--------|--------|-----|
| .NET | 9.0 | Framework |
| Entity Framework Core | 9.0.0 | ORM |
| SQL Server | 9.0.0 | Banco de dados |
| FluentValidation | 12.1.1 | Validação |
| Swashbuckle | 7.2.0 | Swagger/OpenAPI |

## 📦 Padrões Implementados

✅ **Repository Pattern** - Abstração completa de acesso a dados  
✅ **Unit of Work** - Gerenciamento de transações  
✅ **Result Pattern** - Retorno seguro sem exceções  
✅ **Dependency Injection** - Inversão de controle total  
✅ **DTO Pattern** - Separação domínio/apresentação  
✅ **Clean Architecture** - Camadas bem definidas  
✅ **SOLID Principles** - Todos os 5 princípios

## 🎯 Estrutura de Retorno (Result Pattern)

```csharp
// Sucesso
Result<UserDto>.Success(userDto, "Usuário criado com sucesso");

// Falha
Result<UserDto>.Failure("Email já cadastrado");

// No Controller
return result.IsSuccess ? Ok(result) : BadRequest(result);
```

## 🧪 TODO - Testes

```bash
# Criar projetos de teste
dotnet new xunit -n Domain.Tests
dotnet new xunit -n Application.Core.Tests
dotnet new xunit -n Infrastructure.Tests
dotnet new xunit -n API.Tests
```

## 📚 Documentação Adicional

- [SOLID_ARCHITECTURE.md](SOLID_ARCHITECTURE.md) - Princípios SOLID detalhados
- [Swagger UI](https://localhost:5001/swagger) - Documentação interativa da API

## 👥 Convenções de Código

1. ✅ Use **async/await** em todos os métodos de I/O
2. ✅ **CancellationToken** em todos os métodos assíncronos
3. ✅ **Try/catch** em operações de escrita
4. ✅ **Validação** antes de persistir
5. ✅ **Result Pattern** para retornos
6. ✅ **DTOs** para comunicação entre camadas
7. ✅ **Data Annotations** nas entidades
8. ✅ Métodos **privados** para mapeamento (MapToDto)

## 🚀 Próximos Passos

- [ ] Implementar autenticação JWT
- [ ] Adicionar projetos de testes unitários
- [ ] Implementar logging (Serilog)
- [ ] Adicionar Health Checks
- [ ] Implementar paginação
- [ ] Adicionar cache (Redis)
- [ ] Implementar CQRS (opcional)

## 📄 Licença

MIT License
