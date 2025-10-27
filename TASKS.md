# Задачи разработки Keep2DFilesBot

## Фаза 1: Инициализация проекта

### 1.1 Создание структуры проекта
- [x] Создать solution файл
- [x] Создать проект Worker Service (.NET 9)
- [ ] Настроить .editorconfig (уже есть)
- [ ] Добавить .gitignore (уже есть)
- [x] Создать README.md с описанием проекта

### 1.2 Установка NuGet пакетов
- [x] Telegram.Bot (последняя версия)
- [x] Serilog.AspNetCore
- [x] Serilog.Sinks.Console
- [x] Serilog.Sinks.File
- [x] Polly
- [x] Microsoft.Extensions.Http.Polly
- [x] Microsoft.Extensions.Configuration.EnvironmentVariables
- [ ] FluentValidation (опционально для валидации команд)
- [ ] NetArchTest.Rules (опционально для архитектурных проверок)

### 1.3 Конфигурация проекта
- [x] Настроить использование Nullable reference types
- [x] Настроить Central Package Management
- [x] Включить ImplicitUsings для .NET 9
- [x] Настроить C# 13 features

---

## Фаза 2: Базовая архитектура

### 2.1 Value Objects и общие модели
- [x] Создать UserId (Value Object)
- [x] Создать Url (Value Object)
- [x] Создать FileMetadata (модель метаданных)
- [ ] Подготовить модели статистики скачиваний (при необходимости)

### 2.2 Конфигурация приложения
- [x] Создать BotConfiguration (Token, AllowedUsers, WorkingDirectory)
- [x] Создать DownloadConfiguration (MaxFileSize, Timeout, RetryCount)
- [x] Создать StorageConfiguration (BasePath, OrganizationPattern)
- [x] Настроить Options pattern для конфигурации
- [x] Создать appsettings.json
- [x] Создать appsettings.Development.json

### 2.3 Dependency Injection
- [x] Настроить Pure DI в Program.cs
- [x] Зарегистрировать все сервисы через Constructor Injection
- [x] Настроить HttpClient с Polly policies
- [x] Настроить Serilog как основной logger

---

## Фаза 3: Core Services

### 3.1 Worker и обработка Telegram
- [x] Реализовать инициализацию бота
- [x] Настроить Long Polling
- [x] Обработка входящих сообщений
- [x] Обработка команд (/start, /help, /stats)
- [x] Вынести обработку /start, /help, /stats в Features/Commands/*
- [x] Настроить централизованный роутинг команд в Worker
- [x] Обработка URL в сообщениях
- [x] Отправка уведомлений пользователю

### 3.2 Feature: DownloadFile
- [x] Валидация URL через Value Object `Url`
- [x] Скачивание файла через HttpClientFactory с Polly
- [ ] Поддержка progress reporting (при необходимости)
- [x] Определение имени файла и Content-Type
- [x] Обработка таймаутов и ограничений по размеру
- [x] Логирование процесса скачивания
- [ ] Добавить FluentValidation для DownloadFileCommand (опционально)

### 3.3 Infrastructure: Storage
- [x] Создание структуры директорий
- [x] Сохранение файла в файловую систему
- [x] Организация файлов (по дате/пользователю)
- [x] Генерация уникальных имен файлов
- [x] Сохранение метаданных (JSON файл рядом)
- [x] Создать MetadataStorage для работы с JSON-метаданными
- [ ] Реализовать накопление статистики скачиваний
- [ ] Проверка доступного места на диске
- [ ] Очистка временных файлов

### 3.4 WhitelistService
- [ ] Проверка пользователя по ID
- [ ] Загрузка whitelist из конфигурации
- [ ] Поддержка режимов: публичный/whitelist-only
- [ ] Логирование попыток доступа
- [ ] Добавление/удаление пользователей (админ-команды)

---

## Фаза 4: Дополнительные функции

### 4.1 Команды бота
- [ ] `/start` - приветствие и инструкция
- [ ] `/help` - справка по использованию
- [ ] `/stats` - статистика скачиваний (для пользователя)
- [ ] `/admin_stats` - общая статистика (для админа)
- [ ] `/admin_whitelist_add <user_id>` - добавить в whitelist
- [ ] `/admin_whitelist_remove <user_id>` - удалить из whitelist
- [ ] `/admin_whitelist_list` - показать whitelist
- [ ] Реализовать сбор и отображение статистики скачиваний в командах

### 4.2 Обработка ошибок
- [x] Обработка недоступных URL
- [x] Обработка таймаутов
- [x] Обработка ошибок сети
- [ ] Обработка нехватки места на диске
- [x] Информирование пользователя об ошибках
- [ ] Централизованная обработка исключений

### 4.3 Логирование и мониторинг
- [x] Структурированное логирование через Serilog
- [x] Логирование всех операций скачивания
- [x] Логирование ошибок с контекстом
- [x] Ротация лог-файлов
- [ ] Health checks endpoint (для Docker)

---

## Фаза 5: Контейнеризация

### 5.1 Dockerfile
- [ ] Создать multi-stage Dockerfile
- [ ] Использовать официальный образ .NET 9
- [ ] Оптимизировать размер образа
- [ ] Настроить non-root пользователя
- [ ] Настроить volumes для данных и логов
- [ ] Настроить HEALTHCHECK

### 5.2 Docker Compose
- [ ] Создать docker-compose.yml
- [ ] Настроить environment variables
- [ ] Настроить volumes для persistence
- [ ] Настроить restart policy
- [ ] Настроить сеть (при необходимости)
- [ ] Добавить docker-compose.override.yml для dev

### 5.3 Environment конфигурация
- [ ] Создать .env.example
- [ ] Документировать все переменные окружения
- [ ] Настроить секреты (bot token)

---

## Фаза 6: CI/CD

### 6.1 GitHub Actions - Build & Test
- [ ] Создать workflow для build
- [ ] Настроить dotnet restore
- [ ] Настроить dotnet build
- [ ] Настроить dotnet test (когда будут тесты)
- [ ] Проверка форматирования (dotnet format)
- [ ] Кеширование NuGet пакетов

### 6.2 GitHub Actions - Docker
- [ ] Сборка Docker образа
- [ ] Пуш образа в GitHub Container Registry
- [ ] Тегирование образов (latest, version, commit SHA)
- [ ] Очистка старых образов

### 6.3 GitHub Actions - Deploy
- [ ] Подключение к целевому серверу (SSH)
- [ ] Pull нового образа
- [ ] Остановка старого контейнера
- [ ] Запуск нового контейнера
- [ ] Проверка health check
- [ ] Rollback при неудаче

---

## Фаза 7: Документация

### 7.1 README.md
- [x] Описание проекта
- [x] Требования (Docker, .NET 9)
- [x] Инструкция по установке
- [x] Конфигурация (environment variables)
- [ ] Примеры использования
- [x] Команды бота
- [ ] Troubleshooting

### 7.2 DEPLOYMENT.md
- [ ] Инструкция по развертыванию на VPS
- [ ] Настройка Docker и Docker Compose
- [ ] Настройка systemd service (опционально)
- [ ] Настройка логирования
- [ ] Backup и восстановление данных
- [ ] Обновление версий

### 7.3 ARCHITECTURE.md
- [ ] Описание архитектуры решения
- [ ] Диаграммы компонентов
- [ ] Описание потоков данных
- [ ] Технические решения

---

## Фаза 8: Тестирование

### 8.1 Unit тесты
- [ ] Тесты для DownloadService
- [ ] Тесты для StorageService
- [ ] Тесты для WhitelistService
- [ ] Тесты для валидации URL
- [ ] Тесты для обработки команд

### 8.2 Integration тесты
- [ ] Тесты взаимодействия с Telegram API (mock)
- [ ] Тесты скачивания файлов
- [ ] Тесты сохранения файлов
- [ ] Тесты end-to-end сценариев
- [ ] Тесты обработки команд Telegram
- [ ] Архитектурные тесты (NetArchTest)

### 8.3 Тестирование в Docker
- [ ] Проверка работы в контейнере
- [ ] Проверка volumes
- [ ] Проверка environment variables
- [ ] Проверка health checks

---

## Фаза 9: Оптимизация и улучшения

### 9.1 Производительность
- [ ] Асинхронная обработка запросов
- [ ] Параллельное скачивание (для нескольких URL)
- [ ] Оптимизация размера буфера для скачивания
- [ ] Мониторинг использования памяти

### 9.2 Безопасность
- [ ] Валидация URL (защита от SSRF)
- [ ] Ограничение размера файлов (опционально)
- [ ] Rate limiting для пользователей
- [ ] Санитизация имен файлов

### 9.3 Дополнительные возможности
- [ ] Поддержка Head-запроса для проверки размера
- [ ] Предпросмотр информации о файле перед скачиванием
- [ ] Статистика использования
- [ ] Экспорт метаданных (JSON/CSV)
- [ ] Webhook режим (альтернатива polling)

---

## Приоритеты реализации

### Критические (MVP)
- Фаза 1: Инициализация проекта
- Фаза 2: Базовая архитектура
- Фаза 3: Core Services (3.1, 3.2, 3.3)
- Фаза 5: Контейнеризация (5.1, 5.2)

### Важные
- Фаза 3: Core Services (3.4)
- Фаза 4: Дополнительные функции (4.1, 4.2, 4.3)
- Фаза 6: CI/CD
- Фаза 7: Документация

### Дополнительные
- Фаза 8: Тестирование
- Фаза 9: Оптимизация и улучшения

---

## Технологический стек

### Runtime & Framework
- .NET 9.0
- C# 13
- ASP.NET Core Worker Service

### Основные библиотеки
- Telegram.Bot
- Serilog
- Polly
- System.Text.Json

### DevOps
- Docker & Docker Compose
- GitHub Actions
- GitHub Container Registry

### Инструменты разработки
- Visual Studio 2022 / JetBrains Rider
- Git
- Docker Desktop
