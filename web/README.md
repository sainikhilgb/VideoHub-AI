# VideoHub AI Frontend Foundation

This is the production-ready frontend foundation for VideoHub AI, built using React 19, Vite, Tailwind CSS v4, and React Router v7.

## Technology Stack

- **Core**: React 19, TypeScript
- **Bundler**: Vite 8
- **Styling**: Tailwind CSS v4 (configured via `@tailwindcss/vite` plugin)
- **Routing**: React Router v7 (configured with ProtectedRoute & PublicRoute guards)
- **State Management**: TanStack Query (v5), Auth Context (in-memory access tokens), & UI Context (theme & sidebar state)
- **HTTP Client**: Axios (configured with request/response interceptors and error handling)
- **Icons**: Lucide React
- **Formatting & Linting**: ESLint, Prettier

## Folder Structure

```text
web/
├── src/
│   ├── app/                 # App-wide configurations (Router, Layouts, Providers)
│   │   ├── layouts/         # Base layouts (AppLayout)
│   │   ├── providers/       # Global React context providers (UIProvider)
│   │   └── router/          # Route configs mapping pages
│   ├── features/            # Feature-based business modules
│   │   ├── auth/            # LoginPage, RegisterPage, contexts, and route guards
│   │   ├── dashboard/       # Dashboard analytics and charts UI
│   │   ├── projects/        # Project uploads and detail previews
│   │   ├── media/           # Media catalogs
│   │   ├── transcript/      # Speech transcripts preview
│   │   ├── captions/        # Subtitle captions settings
│   │   ├── translation/     # Localization
│   │   ├── jobs/            # Hangfire background tasks monitor
│   │   └── settings/        # App and user settings forms
│   ├── shared/              # Shared assets, styles, hooks, and helpers
│   │   ├── components/      # Common UI elements
│   │   │   ├── layout/      # Layout containers (NotFoundPage)
│   │   │   └── ui/          # Atomic reusable UI components
│   │   ├── services/api/    # Axios client with interceptors
│   │   └── styles/          # Custom styles
│   ├── App.tsx              # Root component with providers
│   ├── index.css            # Base Tailwind v4 style directives
│   └── main.tsx             # Application entry point
```

## Reusable UI Components Implemented

The following reusable UI components are located under `src/shared/components/ui/` and ready for use:

- `AppLogo`: Application branding.
- `Sidebar` & `SidebarItem`: Fully responsive side navigation.
- `Navbar`: Top bar containing search, breadcrumbs, notifications, and profile menus.
- `PageHeader`: Structured page title, subtitle, and action buttons.
- `DashboardCard`: Stat display with trend analysis.
- `SectionCard`: Container with structured header and body.
- `SearchBar`: Standard search input.
- `Breadcrumb`: Interactive path indicators.
- `StatusBadge`: Project and task execution state labels.
- `LoadingSpinner`: SVG spin loaders.
- `EmptyState`: Empty resource placeholder display.
- `ErrorState`: Failure display with a retry trigger.
- `ConfirmDialog`: Action confirmation modal.
- `ThemeToggle`: Theme switcher.
- `AvatarMenu`: Profile settings dropdown.

## Getting Started

### 1. Installation

Run the following command in this directory to install dependencies:

```bash
npm install
```

### 2. Run the Development Server

Start Vite dev server locally:

```bash
npm run dev
```

### 3. Build & Compile

Build the production bundles:

```bash
npm run build
```

### 4. Code Formatting & Linting

Run prettier formatting:

```bash
npm run format
```

Run ESLint check:

```bash
npm run lint
```
