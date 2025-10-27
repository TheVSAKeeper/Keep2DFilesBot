# Keep2DFilesBot

Telegram бот для скачивания файлов по прямым ссылкам и сохранения их в файловую систему.

## Описание

Keep2DFilesBot - это .NET 9 Worker Service приложение, которое работает как Telegram бот. Пользователи могут отправлять прямые ссылки на файлы, и бот автоматически скачает их и сохранит в организованную структуру директорий.

## Основные возможности

- Автоматическое скачивание файлов по прямым ссылкам
- Организация файлов по дате и пользователям
- Поддержка whitelist для ограничения доступа
- Структурированное логирование операций
- Retry logic для обработки сетевых ошибок
- Контейнеризация через Docker

## Требования

- .NET 9.0 SDK
- Docker (опционально для развертывания)

## Установка и запуск

### Локальная разработка

1. Клонировать репозиторий
2. Создать `appsettings.json` с конфигурацией
3. Запустить через `dotnet run`

### Docker

```bash
docker build -t keep2dfilesbot .
docker run -d --name keep2dfilesbot -v ./data:/app/data keep2dfilesbot
```

## Конфигурация

Конфигурация выполняется через `appsettings.json` или переменные окружения:

```json
{
  "BotConfiguration": {
    "Token": "YOUR_BOT_TOKEN",
    "AllowedUsers": ["123456789"],
    "WorkingDirectory": "./data"
  },
  "DownloadConfiguration": {
    "MaxFileSize": 104857600,
    "Timeout": 300,
    "RetryCount": 3
  }
}
```

## Команды бота

- `/start` - Приветствие и инструкция
- `/help` - Справка по использованию
- `/stats` - Статистика скачиваний

## Структура проекта

```
Keep2DFilesBot/
├── Keep2DFilesBot.csproj     # Основной проект
├── Program.cs                 # Точка входа и DI конфигурация
├── Worker.cs                  # Основной worker сервис
├── Services/                  # Бизнес-логика
├── Models/                    # Модели данных
└── Configuration/             # Конфигурационные классы
```

## Лицензия

MIT License
