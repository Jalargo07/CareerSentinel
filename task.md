# Plan de Desarrollo: Playwright LinkedIn Authentication

## Feature: Autenticaci�n de LinkedIn con Playwright

El objetivo es que el scraper de LinkedIn use cookies de una cuenta dummy para evitar captchas y rate limits.

---

## Fase 1: Dependencias y Configuraci�n

- [x] Tarea 1: Agregar NuGet package Microsoft.Playwright al proyecto
  - Archivos: src/CareerSentinel/CareerSentinel.csproj
  - Criterio: dotnet build compila sin errores
  - Dependencias: Ninguna

- [ ] Tarea 2: Agregar campo CookiesPath a LinkedInSettings en AppSettings.cs
  - Archivos: src/CareerSentinel/Configuration/AppSettings.cs
  - Criterio: Compila sin warnings
  - Dependencias: Ninguna

- [ ] Tarea 3: Agregar default CookiesPath en Program.cs EnsureAppSettingsExists
  - Archivos: src/CareerSentinel/Program.cs
  - Criterio: Compila sin warnings
  - Dependencias: Tarea 2

---

## Fase 2: Servicios de Autenticaci�n

- [x] Tarea 4: Crear Services/CookiesManager.cs � persistencia de cookies
  - Archivos: src/CareerSentinel/Services/CookiesManager.cs (nuevo)
  - M�todos: SaveCookiesAsync, LoadCookiesAsync, DeleteCookiesAsync, Exists
  - Criterio: Compila sin warnings
  - Dependencias: Ninguna

- [x] Tarea 5: Crear interfaz Services/ILinkedInAuthService.cs
  - Archivos: src/CareerSentinel/Services/ILinkedInAuthService.cs (nuevo)
  - M�todos: EnsureAuthenticatedAsync(), GetCookiesAsync(), IsAuthenticatedAsync()
  - Criterio: Compila sin warnings
  - Dependencias: Ninguna

- [x] Tarea 6: Crear Services/LinkedInAuthService.cs � autenticaci�n con Playwright
  - Archivos: src/CareerSentinel/Services/LinkedInAuthService.cs (nuevo)
  - Responsabilidades:
    - EnsureAuthenticatedAsync(): Intenta cargar cookies ? si inv�lidas, abre navegador para login manual
    - ExtractCookiesAsync(): Extrae cookies del contexto Playwright
    - GetCookiesAsync(): Retorna cookies v�lidas
    - IsAuthenticatedAsync(): Verifica si las cookies son v�lidas
  - Criterio: Compila sin warnings
  - Dependencias: Tarea 4, Tarea 5

---

## Fase 3: Integraci�n con Scraper

- [x] Tarea 7: Modificar LinkedInScraper para usar cookies de autenticaci�n
  - Archivos: src/CareerSentinel/Services/LinkedInScraper.cs
  - Cambios:
    - Recibir ILinkedInAuthService por constructor
    - Llamar EnsureAuthenticatedAsync() antes de scraping
    - Agregar cookies a los requests HTTP
  - Criterio: Compila sin warnings; scraper usa auth service
  - Dependencias: Tarea 5, Tarea 6

---

## Fase 4: Configuraci�n de DI y Men�

- [x] Tarea 8: Registrar servicios de autenticaci�n en Program.cs
  - Archivos: src/CareerSentinel/Program.cs
  - Registrar CookiesManager + ILinkedInAuthService / LinkedInAuthService en DI
  - Criterio: Compila sin warnings
  - Dependencias: Tarea 5, Tarea 6

- [x] Tarea 9: Agregar opci�n de men� "Configurar autenticaci�n LinkedIn"
  - Archivos: src/CareerSentinel/Services/ConsoleMenu.cs
  - Nueva opci�n en men� para invocar EnsureAuthenticatedAsync()
  - Mostrar estado de cookies (si existen, v�lidas, etc.)
  - Criterio: Men� muestra la opci�n; flujo funcional
  - Dependencias: Tarea 6

- [x] Tarea 10: Integrar opci�n de men� en Program.cs
  - Archivos: src/CareerSentinel/Program.cs
  - Agregar case para la nueva opci�n del men�
  - Criterio: Compila sin warnings; opci�n funcional
  - Dependencias: Tarea 9

---

## Fase 5: Manejo de Errores y Edge Cases

- [x] Tarea 11: Implementar manejo de errores en LinkedInAuthService
  - Archivos: src/CareerSentinel/Services/LinkedInAuthService.cs
  - Detalles:
    - Si cookies expiran ? re-autenticar autom�ticamente
    - Si usuario cierra navegador ? reintentar con mensaje
    - Timeout de 120s para login manual
    - Si Playwright no est� instalado ? mensaje claro
    - Fallback a modo sin cookies si todo falla
  - Criterio: Flujo degrada gracefully
  - Dependencias: Tarea 6

- [x] Tarea 12: Agregar validaci�n de cookies y retry en LinkedInScraper
  - Archivos: src/CareerSentinel/Services/LinkedInScraper.cs
  - Detalles:
    - Si respuesta contiene authwall ? invalidar cookies y re-autenticar
    - Retry una vez despu�s de re-autenticar
  - Criterio: Scraper maneja autenticaci�n fallida gracefully
  - Dependencias: Tarea 7, Tarea 11

---

## Orden de Implementaci�n Sugerido

1. Tarea 1 ? 2 ? 3 (configuraci�n base)
2. Tarea 4 ? 5 ? 6 (servicios de auth)
3. Tarea 7 (integraci�n scraper)
4. Tarea 8 (DI)
5. Tarea 9 ? 10 (men�)
6. Tarea 11 ? 12 (manejo de errores)

---

## Resumen

| # | Fase | Tarea | Estado |
|---|------|-------|--------|
| 1 | Config | NuGet Playwright | ✅ |
| 2 | Config | CookiesPath en AppSettings | ? |
| 3 | Config | Default en appsettings | ? |
| 4 | Services | CookiesManager | ✅ |
| 5 | Services | ILinkedInAuthService | ✅ |
| 6 | Services | LinkedInAuthService | ✅ |
| 7 | Integration | Modificar LinkedInScraper | ✅ |
| 8 | DI | Registrar en Program.cs | ✅ |
| 9 | Menu | Opci�n en ConsoleMenu | ✅ |
| 10 | Menu | Integrar en Program.cs | ✅ |
| 11 | Errors | Manejo de errores auth | ✅ |
| 12 | Errors | Retry con authwall | ✅ |
