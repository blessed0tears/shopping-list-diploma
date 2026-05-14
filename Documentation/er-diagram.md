# ER-диаграмма базы данных

## Описание

ER-диаграмма отражает структуру базы данных приложения **«Совместный список покупок»**. В центре модели находится пользователь Identity (`APPLICATION_USER`), который может состоять в нескольких группах через таблицу `GROUP_MEMBER`. Внутри групп создаются списки покупок, внутри списков — товары. Для товаров хранится история действий.

```mermaid
erDiagram
    APPLICATION_USER ||--o{ GROUP_MEMBER : "состоит в"
    APPLICATION_USER ||--o{ SHOPPING_GROUP : "владеет"
    APPLICATION_USER ||--o{ SHOPPING_ITEM : "добавил"
    APPLICATION_USER ||--o{ SHOPPING_ITEM : "отметил купленным"
    APPLICATION_USER ||--o{ ITEM_HISTORY : "выполнил действие"

    SHOPPING_GROUP ||--o{ GROUP_MEMBER : "имеет участников"
    SHOPPING_GROUP ||--o{ SHOPPING_LIST : "содержит списки"
    SHOPPING_LIST ||--o{ SHOPPING_ITEM : "содержит товары"
    SHOPPING_ITEM ||--o{ ITEM_HISTORY : "имеет историю"

    APPLICATION_USER {
        string Id PK
        string UserName
        string Email
        string DisplayName
        datetime CreatedAtUtc
    }

    SHOPPING_GROUP {
        int Id PK
        string Name
        string Description
        string OwnerId FK
        datetime CreatedAtUtc
    }

    GROUP_MEMBER {
        int Id PK
        int ShoppingGroupId FK
        string ApplicationUserId FK
        string Role
        datetime JoinedAtUtc
    }

    SHOPPING_LIST {
        int Id PK
        int ShoppingGroupId FK
        string Name
        bool IsArchived
        datetime CreatedAtUtc
    }

    SHOPPING_ITEM {
        int Id PK
        int ShoppingListId FK
        string Name
        decimal Quantity
        string Unit
        string Category
        string Comment
        bool IsPurchased
        datetime CreatedAtUtc
        datetime PurchasedAtUtc
        string CreatedByUserId FK
        string PurchasedByUserId FK
    }

    ITEM_HISTORY {
        int Id PK
        int ShoppingItemId FK
        string ApplicationUserId FK
        string Action
        string OldValue
        string NewValue
        datetime CreatedAtUtc
    }
```

## Ключевые ограничения

- Пользователь может быть участником одной группы только один раз.
- Список покупок всегда принадлежит одной группе.
- Товар всегда принадлежит одному списку покупок.
- Категория и комментарий товара хранятся в `SHOPPING_ITEM`.
- Автор добавления товара и пользователь, отметивший товар купленным, хранятся ссылками на `APPLICATION_USER`.
- История действий связана с товаром и пользователем, выполнившим действие.
