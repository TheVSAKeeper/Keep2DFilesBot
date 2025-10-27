# Keep2DFilesBot

Telegram-бот, который принимает прямые ссылки на файлы, скачивает их и сохраняет в организованной файловой структуре с записью метаданных.

## Описание

Keep2DFilesBot — это .NET 9 Worker Service приложение, реализованное по принципам Vertical Slice Architecture. Бот принимает сообщения с HTTP/HTTPS ссылками, скачивает файлы с использованием Polly-политик повторных попыток и сохраняет их в директорию, структурированную по пользователям и дате. Для обработки ошибок применяется Result Pattern, что позволяет строить декларативные пайплайны без выбрасывания исключений.

## Основные возможности

- Скачивание файлов по прямым HTTP/HTTPS ссылкам с проверкой размера и таймаутов
- Нормализация имён и сохранение файлов в структуре `BasePath/<UserId>/<yyyy-MM-dd>/`
- Генерация метаданных (имя, размер, контент-тип, дата загрузки, идентификатор пользователя)
- Настраиваемые ограничения по размеру и таймаутам скачивания
- Структурированное логирование через Serilog (консоль + rolling файл)
- Polly retry/timeout политики для `HttpClient`
- Команды `/start` и `/help`, информирующие пользователя о возможностях бота

## Требования

- .NET 9.0 SDK
- Telegram bot token
- Права записи в директорию, указанную в конфигурации хранения

## Установка и запуск

### Локальная разработка

1. Клонировать репозиторий
2. Настроить `appsettings.json` (см. пример ниже)
3. Установить переменную окружения `TelegramBotToken` или заполнить токен в конфигурации
4. Запустить

```bash
dotnet restore
dotnet run --project Keep2DFilesBot/Keep2DFilesBot.csproj
```

### Логи

По умолчанию логи пишутся в консоль и в файлы `logs/bot-<дата>.txt` с ротацией по дням.

## Конфигурация

Конфигурация выполняется через `appsettings.json` или переменные окружения:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "BotConfiguration": {
    "Token": "ВАШ_ТОКЕН_БОТА",
    "AllowedUsers": [123456789],
    "WorkingDirectory": "./data",
    "IsPublic": false
  },
  "DownloadConfiguration": {
    "MaxFileSize": 104857600,
    "TimeoutSeconds": 300,
    "RetryCount": 3,
    "RetryDelaySeconds": 2
  },
  "StorageConfiguration": {
    "BasePath": "./data/files",
    "DateFormat": "yyyy-MM-dd",
    "SaveMetadata": true
  }
}
```

## Команды бота

- `/start` - Приветствие и инструкция
- `/help` - Справка по использованию
- Отправка прямой ссылки без команды запускает процесс скачивания

## Структура проекта

```
Keep2DFilesBot/
├── Keep2DFilesBot/
│   ├── Program.cs                  # Pure DI, Serilog, HttpClient с Polly
│   ├── Worker.cs                   # Обработка Telegram-обновлений
│   ├── Features/
│   │   └── DownloadFile/           # Команда и обработчик скачивания
│   ├── Infrastructure/
│   │   └── Storage/FileStorage.cs  # Работа с файловой системой
│   └── Shared/
│       ├── Results/                # Result Pattern и расширения
│       ├── Models/                 # Value Objects (UserId, Url, FileMetadata)
│       └── Configuration/          # Options-паттерн для настроек
└── ...
```

## Текущий статус

- Реализована инфраструктура Result Pattern, Value Objects и конфигураций
- Настроен Pure DI с Serilog и Polly
- Обработчик DownloadFile интегрирован с Telegram Worker
- Сохранение файлов и метаданных работает через `FileStorage`
- Следующий шаг — добавить обработку whitelist и дополнительные команды (`/stats`, административные сценарии)

## Лицензия

MIT License
