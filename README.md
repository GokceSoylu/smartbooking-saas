# 📅 SmartBooking SaaS Platform

**SmartBooking** is a modern, multi-tenant appointment and booking management platform built with high performance, scalability, and automated customer communication in mind. It integrates directly with the Meta WhatsApp Graph API to automate appointment confirmations, status updates, and timely reminders to significantly reduce no-show rates.

---

## 🚀 Key Features

- **Multi-Tenancy Architecture**: Secure data isolation and tenant-aware querying using Entity Framework Core.
- **Automated WhatsApp Notifications**: Integrated with Meta Graph API (v22.0) for sending transactional SMS/WhatsApp appointment reminders and status updates (`randevu_alindi`, reminders, etc.).
- **No-Show Engine (Background Worker)**: Scheduled background worker (`AppointmentReminderWorker`) that continuously checks upcoming appointments, triggers notifications, and marks reminders as sent using tenant-agnostic query filtering (`.IgnoreQueryFilters()`).
- **RESTful API**: Scalable backend design following Clean Architecture patterns.
- **Database & Migration Engine**: Robust PostgreSQL database integration with EF Core migration pipelines.

---

## 🛠️ Tech Stack

### **Backend**
- **Framework**: .NET 10 Web API
- **Database**: PostgreSQL 16 (Dockerized)
- **ORM**: Entity Framework Core 10 (Code-First)
- **Cache**: Redis 7
- **Authentication & Security**: JWT Authentication, System User Token management for Meta Graph API

### **Integrations & Messaging**
- **Meta Graph API**: WhatsApp Business Platform v22.0
- **SMS Gateway**: Netgsm Integration

---

## 🏗️ Project Architecture

The project follows a modular, layer-separated monorepo architecture:

```text
smartbooking-saas/
├── apps/
│   ├── api/
│   │   └── src/
│   │       ├── SmartBooking.Api/              # API Controllers & Endpoints
│   │       ├── SmartBooking.Application/      # Business Logic & Interfaces
│   │       ├── SmartBooking.Domain/           # Entities & Domain Models
│   │       └── SmartBooking.Infrastructure/   # DB Context, Meta API, Background Workers