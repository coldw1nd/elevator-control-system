# Elevator Control System

Курсовой проект по дисциплине «Создание программного обеспечения».

## Назначение
Веб-приложение для имитации автоматизированной системы контроля работы лифта в n-этажном здании с одним лифтом.

## Стек технологий

### Backend
- C#
- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- PostgreSQL
- SignalR
- JWT
- Serilog
- ClosedXML

### Frontend
- Vue 3
- TypeScript
- Vite
- Vuetify
- Axios
- SignalR client

## Архитектура
Проект реализован как распределённый модульный монолит:
- frontend — отдельное MPA-приложение;
- backend — отдельный REST API + SignalR сервис;
- база данных — PostgreSQL.

## Запуск backend
```bash
cd backend
dotnet restore ElevatorControlSystem.sln
cd src/ElevatorControlSystem.Api
dotnet run --launch-profile http
```

## Запуск frontend
```bash
cd frontend
npm install
npm run dev
```

## Тестирование

### Backend
```bash
cd backend
dotnet test ElevatorControlSystem.sln
```

### Frontend
```bash
cd frontend
npx vitest run
```

## Адреса
- Backend Swagger: http://localhost:5000/swagger
- Backend Health: http://localhost:5000/api/health
- Frontend: http://localhost:5173/

## Тестовые пользователи
- admin / Admin123!
- operator / Operator123!
- viewer / Viewer123!

## Основные функции
- создание и запуск сеансов моделирования;
- добавление пассажиров в черновике и в реальном времени;
- команда «Ход»;
- визуализация движения лифта;
- контроль перегрузки;
- итоговый отчёт;
- экспорт отчёта в Excel;
- журнал аудита;
- разграничение доступа по ролям.