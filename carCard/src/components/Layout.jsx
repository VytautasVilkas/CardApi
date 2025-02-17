import React from 'react';

const Layout = ({ children }) => {
  return (
    <div className="flex flex-col min-h-screen">
      {/* Main Content */}
      <main className="flex-grow flex items-center justify-center overflow-hidden">
        {children}
      </main>

      {/* Footer */}
      <footer className="w-full  text-Black py-4 text-center">
      <div className="border-t border-[#e0ded8] mt-1"></div>

        {/* Center Section */}
        <div className="relative text-sm text-center pt-1" style={{ marginBottom: "5px" }}>
        © 2024 Visos teisės saugomos
        </div>
      </footer>
    </div>
  );
};

export default Layout;
