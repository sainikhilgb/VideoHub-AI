import React from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from '@/shared/components/ui/Sidebar'
import { Navbar } from '@/shared/components/ui/Navbar'
import { useUI } from '@/app/providers/UIProvider'
import clsx from 'clsx'

export const AppLayout: React.FC = () => {
  const { sidebarOpen, setSidebarOpen } = useUI()

  return (
    <div className="min-h-screen flex bg-animated-gradient text-text-main font-sans">
      {/* 1. Desktop & Tablet Sidebar (hidden on mobile, visible on lg/md) */}
      <div className="hidden lg:block shrink-0">
        <Sidebar />
      </div>

      {/* 2. Tablet Collapsible Sidebar (visible on md, hidden on lg and sm) */}
      <div className="hidden md:block lg:hidden shrink-0">
        <Sidebar />
      </div>

      {/* 3. Mobile Sidebar Drawer (overlay) */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 backdrop-blur-xs lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}
      <div
        className={clsx(
          'fixed inset-y-0 left-0 z-50 transform transition-transform duration-300 lg:hidden shrink-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <Sidebar isMobileDrawer={true} />
      </div>

      {/* Main Content Layout Container */}
      <div className="flex-1 flex flex-col min-w-0 min-h-screen">
        <Navbar />

        {/* Main Content Area */}
        <main className="flex-1 overflow-x-hidden overflow-y-auto p-4 md:p-6 lg:p-8 custom-scrollbar">
          <div className="max-w-7xl mx-auto animate-fade-in">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
