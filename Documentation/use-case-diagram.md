# Диаграмма вариантов использования

## Описание

Диаграмма показывает основные действия пользователей информационной системы **«Совместный список покупок»**.

```mermaid
flowchart LR
    Guest[Гость]
    User[Авторизованный пользователь]
    Owner[Владелец группы]

    Register((Регистрация))
    Login((Вход в систему))
    Logout((Выход из системы))
    ViewGroups((Просмотр своих групп))
    CreateGroup((Создание группы покупок))
    AddMember((Добавление участника по email))
    ViewGroup((Просмотр группы))
    CreateList((Создание списка покупок))
    ViewList((Просмотр списка покупок))
    AddItem((Добавление товара с категорией и комментарием))
    EditItem((Редактирование товара))
    DeleteItem((Удаление товара))
    TogglePurchased((Отметка товара как купленного))
    FilterItems((Фильтрация товаров))
    ViewAudit((Просмотр автора и истории изменений))

    Guest --> Register
    Guest --> Login

    User --> Logout
    User --> ViewGroups
    User --> CreateGroup
    User --> ViewGroup
    User --> CreateList
    User --> ViewList
    User --> AddItem
    User --> EditItem
    User --> DeleteItem
    User --> TogglePurchased
    User --> FilterItems
    User --> ArchiveList((Архивирование списка))
    User --> ViewAudit

    Owner --> AddMember
    Owner --> EditGroup((Редактирование группы))
    Owner --> RemoveMember((Удаление участника))
    Owner --> ViewGroup
    Owner --> CreateList

    Register --> Login
    ViewGroups --> ViewGroup
    ViewGroup --> CreateList
    ViewList --> AddItem
    ViewList --> EditItem
    ViewList --> DeleteItem
    ViewList --> TogglePurchased
    ViewList --> FilterItems
    TogglePurchased --> ViewAudit
```

## Участники

- **Гость** — пользователь без входа в систему.
- **Авторизованный пользователь** — зарегистрированный пользователь, который может работать с доступными ему группами, списками и товарами.
- **Владелец группы** — пользователь, создавший группу. На текущем этапе он может добавлять участников в группу.
