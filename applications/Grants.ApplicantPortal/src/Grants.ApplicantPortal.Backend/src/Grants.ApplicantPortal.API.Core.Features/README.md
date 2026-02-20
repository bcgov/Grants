## Core Project + Core.Features (Domain Model)

In Clean Architecture, the central focus should be on Entities and business rules.

In Domain-Driven Design, this is the Domain Model.

This project should contain all of your Entities, Value Objects, and business logic.

Entities that are related and should change together should be grouped into an Aggregate.

Entities should leverage encapsulation and should minimize public setters.

Entities can leverage Domain Events to communicate changes to other parts of the system.

Entities can define Specifications that can be used to query for them.

For mutable access, Entities should be accessed through a Repository interface.

Read-only ad hoc queries can use separate Query Services that don't use the Domain Model.

## Architecture Overview

This project follows Clean Architecture and Domain-Driven Design principles. The following diagrams illustrate the key patterns and structures:

### Clean Architecture Layers

```mermaid
graph TD
    A[Web Layer<br/>API Controllers] --> B[Use Cases Layer<br/>Application Logic]
    B --> C[Core.Features<br/>Domain Model]
    B --> D[Infrastructure Layer<br/>Data & External Services]
    D --> C
    
    style C fill:#e1f5fe
    style B fill:#f3e5f5
    style A fill:#fff3e0
    style D fill:#e8f5e8
```

### Aggregate Structure

```mermaid
graph TB
    subgraph "Aggregate Root"
        AR[Entity<br/>Aggregate Root<br/>IAggregateRoot]
        VO1[Value Object 1]
        VO2[Value Object 2]
        AR --> VO1
        AR --> VO2
    end
    
    subgraph "Supporting Components"
        SPEC[Specifications<br/>Query Logic]
        EVENT[Domain Events<br/>Side Effects]
        HANDLER[Event Handlers<br/>Domain Reactions]
        SERVICE[Domain Services<br/>Business Logic]
    end
    
    AR -.-> SPEC
    AR -.-> EVENT
    EVENT --> HANDLER
    SERVICE --> AR
    
    style AR fill:#ffeb3b
    style VO1 fill:#81c784
    style VO2 fill:#81c784
    style SPEC fill:#64b5f6
    style EVENT fill:#ff8a65
    style HANDLER fill:#ff8a65
    style SERVICE fill:#ba68c8
```

### Domain Event Flow

```mermaid
sequenceDiagram
    participant Service as Domain Service
    participant Entity as Aggregate Root
    participant Mediator as MediatR
    participant Handler as Event Handler
    participant Infrastructure as Infrastructure Layer
    
    Service->>Entity: Perform business operation
    Entity->>Entity: Update state
    Service->>Mediator: Publish domain event
    Mediator->>Handler: Handle event
    Handler->>Infrastructure: Send email/log/etc
    
    Note over Service,Infrastructure: Domain events enable loose coupling<br/>between aggregates and side effects
```

### Repository Pattern

```mermaid
graph LR
    subgraph "Core Layer"
        ENTITY[Aggregate Root]
        IREPO[IRepository Interface]
    end
    
    subgraph "Infrastructure Layer"
        REPO[EF Repository Implementation]
        DBCONTEXT[DbContext]
    end
    
    subgraph "Application Layer"
        USECASE[Use Case]
    end
    
    USECASE --> IREPO
    IREPO -.-> REPO
    REPO --> DBCONTEXT
    REPO --> ENTITY
    
    style ENTITY fill:#ffeb3b
    style IREPO fill:#e1f5fe
    style REPO fill:#e8f5e8
    style DBCONTEXT fill:#e8f5e8
    style USECASE fill:#f3e5f5
```

### Specification Pattern

```mermaid
graph TD
    subgraph "Query Construction"
        SPEC1[ByIdSpecification]
        SPEC2[ActiveItemsSpecification]
        SPEC3[CustomSpecification]
    end
    
    subgraph "Repository"
        REPO[Repository]
        QUERY[LINQ Query]
    end
    
    subgraph "Database"
        DB[(Database)]
    end
    
    SPEC1 --> REPO
    SPEC2 --> REPO
    SPEC3 --> REPO
    REPO --> QUERY
    QUERY --> DB
    
    style SPEC1 fill:#64b5f6
    style SPEC2 fill:#64b5f6
    style SPEC3 fill:#64b5f6
    style REPO fill:#81c784
    style QUERY fill:#fff176
    style DB fill:#e0e0e0
```

## Key Patterns Implemented

### 1. Aggregate Pattern
- **Aggregate Root**: Main entity that enforces business rules
- **Value Objects**: Immutable objects that describe aspects of the domain
- **Encapsulation**: Private setters with public methods for state changes

### 2. Domain Events
- **Event Publishing**: Aggregates publish events when state changes
- **Event Handlers**: React to domain events with side effects
- **Loose Coupling**: Events decouple business logic from infrastructure concerns

### 3. Specifications
- **Query Logic**: Encapsulate complex query logic in reusable specifications
- **Repository Integration**: Use specifications with repository pattern
- **Testability**: Specifications make query logic unit testable

### 4. Domain Services
- **Cross-Aggregate Operations**: Handle operations that span multiple aggregates
- **Complex Business Rules**: Implement domain logic that doesn't belong in a single entity
- **Event Coordination**: Orchestrate domain events across operations

## Current Domain Model

The project currently contains the following aggregates:

### Profile Aggregate
- **Profile**: Main entity representing a user profile in the system
- **Purpose**: Manages user identity and authentication data
- **Location**: `Profiles/ProfileAggregate/`

## Implementation Guidelines

1. **New Aggregates**: Create new aggregates following the Profile pattern
2. **Value Objects**: Use for concepts that don't have identity (like addresses, money, etc.)
3. **Domain Events**: Publish events for significant business state changes
4. **Specifications**: Create specifications for complex queries
5. **Domain Services**: Use for operations that don't fit in a single aggregate

## Examples and Resources

Need help? Check out the sample here:
https://github.com/ardalis/CleanArchitecture/tree/main/sample

Still need help?
Contact us at https://nimblepros.com
