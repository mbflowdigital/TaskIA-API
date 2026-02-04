# Arquitetura SOLID - TaskIA

## 🎯 Princípios SOLID Implementados

### 1️⃣ **S - Single Responsibility Principle (SRP)**

Cada classe tem uma única responsabilidade:

```
✅ UserService        → Lógica de negócio de usuários
✅ UserController     → Receber requisições HTTP e retornar respostas
✅ UserRepository     → Acesso a dados de usuários
✅ UnitOfWork         → Gerenciar transações
✅ UserValidator      → Validar dados de entrada
```

**Exemplo:**
```csharp
// ✅ CORRETO - Responsabilidade única
public class UserService : IUserService
{
    // Apenas lógica de negócio de usuários
    public async Task<Result<UserDto>> CreateAsync(...)
    {
        // Validação, regras de negócio, orquestração
    }
}

// ❌ ERRADO - Múltiplas responsabilidades
public class UserService
{
    public void CreateUser() { }
    public void SendEmail() { }  // Deveria ser EmailService
    public void LogActivity() { } // Deveria ser Logger
}
```

---

### 2️⃣ **O - Open/Closed Principle (OCP)**

Aberto para extensão, fechado para modificação:

```csharp
// ✅ Extensível via herança/interface
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    // Novas entidades podem estender sem modificar BaseEntity
}

public class User : BaseEntity
{
    // Adiciona propriedades específicas sem modificar BaseEntity
    public string Name { get; private set; }
}

// ✅ Extensível via Strategy Pattern
public interface IRepository<T> where T : BaseEntity
{
    // Nova implementação? Crie uma nova classe que implementa IRepository
}
```

**Exemplo de extensão:**
```csharp
// Nova funcionalidade sem modificar código existente
public class UserAuditService : IUserService
{
    private readonly IUserService _innerService;
    private readonly IAuditLogger _auditLogger;

    public async Task<Result<UserDto>> CreateAsync(...)
    {
        var result = await _innerService.CreateAsync(...);
        await _auditLogger.LogAsync("User created");
        return result;
    }
}
```

---

### 3️⃣ **L - Liskov Substitution Principle (LSP)**

Subtipos devem ser substituíveis por seus tipos base:

```csharp
// ✅ Qualquer IRepository<User> pode ser substituído
IRepository<User> repo1 = new Repository<User>(context);
IRepository<User> repo2 = new CachedRepository<User>(context, cache);
IRepository<User> repo3 = new MockRepository<User>(); // Para testes

// Todos funcionam da mesma forma
var user = await repo1.GetByIdAsync(id);
var user = await repo2.GetByIdAsync(id);
var user = await repo3.GetByIdAsync(id);
```

**Regra:** Implementações não devem quebrar contratos da interface.

---

### 4️⃣ **I - Interface Segregation Principle (ISP)**

Clientes não devem depender de métodos que não usam:

```csharp
// ✅ CORRETO - Interfaces específicas
public interface IUserService
{
    Task<Result<UserDto>> CreateAsync(...);
    Task<Result<UserDto>> GetByIdAsync(...);
    // Apenas métodos relevantes para usuários
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
    // Apenas métodos de email
}

// ❌ ERRADO - Interface "gordinha"
public interface IUserAndEmailAndLogService
{
    Task CreateUser(...);
    Task SendEmail(...);
    Task LogActivity(...);
    // Cliente que só precisa de email é forçado a conhecer tudo
}
```

**Nossa implementação:**
```csharp
// Interfaces segregadas por responsabilidade
IUserService        → Operações de usuário
IRepository<T>      → Operações de persistência
IUnitOfWork         → Transações
IValidator<T>       → Validações
```

---

### 5️⃣ **D - Dependency Inversion Principle (DIP)**

**Módulos de alto nível NÃO devem depender de módulos de baixo nível. Ambos devem depender de abstrações.**

#### 📊 Diagrama de Dependências

```
┌─────────────────────────────────────┐
│  Controllers (Alto Nível)           │
│  Depende de: IUserService          │ ← Abstração
└────────────┬────────────────────────┘
             │ depende de
             ▼
┌─────────────────────────────────────┐
│  IUserService (Abstração/Interface) │
└────────────┬────────────────────────┘
             │ implementada por
             ▼
┌─────────────────────────────────────┐
│  UserService (Implementação)        │
│  Depende de: IRepository<User>     │ ← Abstração
└────────────┬────────────────────────┘
             │ depende de
             ▼
┌─────────────────────────────────────┐
│  IRepository<T> (Abstração)        │
└────────────┬────────────────────────┘
             │ implementada por
             ▼
┌─────────────────────────────────────┐
│  Repository<T> (Implementação)      │
│  Depende de: DbContext             │
└─────────────────────────────────────┘
```

#### ✅ Implementação Correta

**Controller → Interface (Não implementação concreta)**
```csharp
public class UsersController : ControllerBase
{
    private readonly IUserService _userService; // ✅ Interface

    public UsersController(IUserService userService) // ✅ DI recebe interface
    {
        _userService = userService;
    }
}
```

**Service → Interface (Não implementação concreta)**
```csharp
public class UserService : IUserService
{
    private readonly IRepository<User> _repository;     // ✅ Interface
    private readonly IUnitOfWork _unitOfWork;          // ✅ Interface

    public UserService(
        IRepository<User> repository,     // ✅ DI recebe interface
        IUnitOfWork unitOfWork)           // ✅ DI recebe interface
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
}
```

**DI Container registra Interface → Implementação**
```csharp
public static IServiceCollection AddApplicationCore(this IServiceCollection services)
{
    // ✅ Registra abstração → implementação
    services.AddScoped<IUserService, UserService>();
    
    return services;
}

public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    // ✅ Registra abstração → implementação
    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    
    return services;
}
```

#### ❌ Implementação INCORRETA (Anti-padrão)

```csharp
// ❌ ERRADO - Depende de implementação concreta
public class UsersController : ControllerBase
{
    private readonly UserService _userService; // Implementação concreta!

    public UsersController(UserService userService)
    {
        _userService = userService;
    }
}

// ❌ ERRADO - Instancia diretamente
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController()
    {
        _userService = new UserService(new Repository(), new UnitOfWork());
        // Acoplamento forte! Difícil testar!
    }
}
```

---

## 🏗️ Fluxo Completo com SOLID

### Requisição HTTP → Response

```
1. HTTP Request
   ↓
2. UsersController (depende de IUserService)
   ↓
3. IUserService (abstração)
   ↓
4. UserService (implementação, depende de IRepository + IUnitOfWork)
   ↓
5. IRepository<User> (abstração)
   ↓
6. Repository<User> (implementação, depende de DbContext)
   ↓
7. DbContext → Database
```

**Todos os pontos de dependência são abstrações!**

---

## 🧪 Benefícios da Arquitetura SOLID

### ✅ **Testabilidade**
```csharp
// Fácil criar mocks de interfaces
var mockUserService = new Mock<IUserService>();
mockUserService
    .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), default))
    .ReturnsAsync(Result<UserDto>.Success(new UserDto()));

var controller = new UsersController(mockUserService.Object);
// Testa controller isoladamente!
```

### ✅ **Manutenibilidade**
- Mudanças em uma camada não afetam outras
- Código organizado e previsível
- Fácil encontrar onde modificar

### ✅ **Extensibilidade**
```csharp
// Adicionar cache sem modificar UserService
public class CachedUserService : IUserService
{
    private readonly IUserService _innerService;
    private readonly ICache _cache;

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, ...)
    {
        var cached = await _cache.GetAsync<UserDto>($"user:{id}");
        if (cached != null) return Result<UserDto>.Success(cached);

        var result = await _innerService.GetByIdAsync(id);
        if (result.IsSuccess)
            await _cache.SetAsync($"user:{id}", result.Data);
        
        return result;
    }
}

// Registrar no DI (sem modificar código existente!)
services.AddScoped<IUserService, CachedUserService>();
```

### ✅ **Substituibilidade**
```csharp
// Trocar de banco de dados? Apenas implemente IRepository
services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

// Mudar estratégia de transação? Implemente IUnitOfWork
services.AddScoped<IUnitOfWork, NoSqlUnitOfWork>();
```

---

## 📝 Checklist SOLID para Novos Recursos

Ao criar uma nova feature, siga:

- [ ] **S** - Crie classes com responsabilidade única
  - [ ] Service para lógica de negócio
  - [ ] Controller para HTTP
  - [ ] Repository para dados
  - [ ] Validator para validações

- [ ] **O** - Use abstrações para permitir extensões
  - [ ] Crie interface antes da implementação
  - [ ] Use métodos virtuais quando herança for necessária

- [ ] **L** - Garanta substituibilidade
  - [ ] Implemente todos os métodos da interface
  - [ ] Não lance exceções inesperadas
  - [ ] Mantenha contratos

- [ ] **I** - Crie interfaces específicas
  - [ ] Interface por responsabilidade
  - [ ] Evite interfaces "gordas"
  - [ ] Cliente só conhece o que precisa

- [ ] **D** - Dependa de abstrações
  - [ ] Controller → IService
  - [ ] Service → IRepository
  - [ ] Registre no DI: `services.AddScoped<IService, Service>()`

---

## 🎓 Exemplo Completo: Adicionar ProductService

```csharp
// 1. Criar a interface (Dependency Inversion)
public interface IProductService
{
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request);
    Task<Result<IEnumerable<ProductDto>>> GetAllAsync();
}

// 2. Implementar (Single Responsibility)
public class ProductService : IProductService
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IRepository<Product> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request)
    {
        var product = new Product(request.Name, request.Price);
        await _repository.AddAsync(product);
        await _unitOfWork.CommitAsync();
        return Result<ProductDto>.Success(MapToDto(product));
    }
    
    // ... outros métodos
}

// 3. Registrar no DI
services.AddScoped<IProductService, ProductService>();

// 4. Usar no Controller (Dependency Inversion)
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }
}
```

---

## 🚀 Conclusão

Nossa arquitetura implementa **todos os 5 princípios SOLID**:

✅ **S** - Classes com responsabilidade única  
✅ **O** - Extensível via interfaces e herança  
✅ **L** - Implementações substituíveis  
✅ **I** - Interfaces segregadas por responsabilidade  
✅ **D** - Todas as dependências são abstrações  

Isso resulta em código **testável, manutenível, extensível e desacoplado**! 🎯
