// Yoklama System - Sidebar Only

class ModernSidebar {
    constructor() {
        this.sidebar = document.getElementById('sidebar');
        this.toggleBtn = null;
        this.mainContent = document.querySelector('.main-content');
        this.isMobileOrTablet = window.innerWidth <= 1024;
        
        // Account sidebar elements
        this.accountSidebar = document.getElementById('accountSidebar');
        this.accountToggle = document.getElementById('accountToggle');
        this.mobileOverlay = null;
        
        this.init();
    }

    init() {
        if (!this.sidebar) return;
        
        this.createToggleButton();
        this.createMobileOverlay();
        this.bindEvents();
        this.setInitialState();
        this.highlightActiveNav();
    }

    createToggleButton() {
        // Create toggle button if it doesn't exist
        this.toggleBtn = document.querySelector('.header-toggle') || document.querySelector('.sidebar-toggle');
        
        if (!this.toggleBtn) {
            const toggleBtn = document.createElement('button');
            toggleBtn.className = 'sidebar-toggle';
            toggleBtn.innerHTML = '<i class="fas fa-bars"></i>';
            toggleBtn.setAttribute('aria-label', 'Toggle Sidebar');
            
            const headerRight = document.querySelector('.content-header .ms-auto');
            if (headerRight) {
                toggleBtn.classList.add('header-toggle');
                headerRight.appendChild(toggleBtn);
            } else {
                document.body.appendChild(toggleBtn);
            }
            
            this.toggleBtn = toggleBtn;
        }
    }

    createMobileOverlay() {
        this.mobileOverlay = document.createElement('div');
        this.mobileOverlay.className = 'mobile-overlay d-md-none';
        this.mobileOverlay.addEventListener('click', () => {
            this.hideAll();
        });
        document.body.appendChild(this.mobileOverlay);
    }

    bindEvents() {
        // Toggle button click
        if (this.toggleBtn) {
            this.toggleBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.toggleSidebar();
            });
        }

        // Account toggle button click
        if (this.accountToggle) {
            this.accountToggle.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.toggleAccountSidebar();
            });
        }

        // Sidebar close button
        const sidebarCloseBtn = document.querySelector('.sidebar-close');
        if (sidebarCloseBtn) {
            sidebarCloseBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.hideSidebar();
            });
        }

        // Account sidebar close button
        const accountCloseBtn = document.querySelector('.account-close');
        if (accountCloseBtn) {
            accountCloseBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.hideAccountSidebar();
            });
        }

        // Window resize handler
        window.addEventListener('resize', () => {
            this.handleResize();
        });

        // Escape key to close on mobile and tablet
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isMobileOrTablet) {
                this.hideAll();
            }
        });

        // Nav link clicks - close mobile and tablet sidebar
        const navLinks = this.sidebar.querySelectorAll('.nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', () => {
                if (this.isMobileOrTablet) {
                    this.hideSidebar();
                }
            });
        });

        // Account menu item clicks - close account sidebar
        const accountMenuItems = document.querySelectorAll('.account-menu-item');
        accountMenuItems.forEach(item => {
            item.addEventListener('click', () => {
                if (this.isMobileOrTablet) {
                    this.hideAccountSidebar();
                }
            });
        });
    }

    setInitialState() {
        if (this.isMobileOrTablet) {
            // Mobile & Tablet: start hidden (off-canvas)
            this.sidebar.classList.remove('show');
            this.updateToggleIcon('bars');
            if (this.toggleBtn) this.toggleBtn.style.display = '';
        } else {
            // Desktop: visible, no hover/collapse behavior
            this.sidebar.classList.remove('show');
            this.sidebar.classList.remove('collapsed');
            if (this.toggleBtn) this.toggleBtn.style.display = 'none';
        }
    }

    toggleSidebar() {
        if (!this.isMobileOrTablet) return; // no toggle on desktop
        if (this.sidebar.classList.contains('show')) {
            this.hideSidebar();
        } else {
            this.showSidebar();
        }
    }

    toggleAccountSidebar() {
        if (!this.isMobileOrTablet) return; // no toggle on desktop
        if (this.accountSidebar && this.accountSidebar.classList.contains('show')) {
            this.hideAccountSidebar();
        } else {
            this.showAccountSidebar();
        }
    }

    collapse() {
        // No-op: collapse behavior removed for desktop
    }

    expand() {
        // No-op: collapse behavior removed for desktop
    }

    showSidebar() {
        if (!this.isMobileOrTablet) return;
        
        // Hide account sidebar first
        this.hideAccountSidebar();
        
        this.sidebar.classList.add('show');
        this.updateToggleIcon('times');
        this.showOverlay();
        document.body.style.overflow = 'hidden';
    }

    hideSidebar() {
        if (!this.isMobileOrTablet) return;
        
        this.sidebar.classList.remove('show');
        this.updateToggleIcon('bars');
        this.hideOverlay();
        document.body.style.overflow = '';
    }

    showAccountSidebar() {
        if (!this.isMobileOrTablet || !this.accountSidebar) return;
        
        // Hide main sidebar first
        this.hideSidebar();
        
        this.accountSidebar.classList.add('show');
        this.showOverlay();
        document.body.style.overflow = 'hidden';
    }

    hideAccountSidebar() {
        if (!this.isMobileOrTablet || !this.accountSidebar) return;
        
        this.accountSidebar.classList.remove('show');
        this.hideOverlay();
        document.body.style.overflow = '';
    }

    showOverlay() {
        if (this.mobileOverlay) {
            this.mobileOverlay.classList.add('show');
        }
    }

    hideOverlay() {
        if (this.mobileOverlay) {
            this.mobileOverlay.classList.remove('show');
        }
    }

    hideAll() {
        this.hideSidebar();
        this.hideAccountSidebar();
    }

    updateToggleIcon(iconName) {
        if (this.toggleBtn) {
            this.toggleBtn.innerHTML = `<i class="fas fa-${iconName}"></i>`;
            if (iconName === 'times' || iconName === 'chevron-left') {
                this.toggleBtn.classList.add('is-open');
            } else {
                this.toggleBtn.classList.remove('is-open');
            }
        }
    }

    handleResize() {
        const wasMobileOrTablet = this.isMobileOrTablet;
        this.isMobileOrTablet = window.innerWidth <= 1024;

        if (wasMobileOrTablet !== this.isMobileOrTablet) {
            if (this.isMobileOrTablet) {
                // Switched to mobile/tablet
                this.sidebar.classList.remove('collapsed');
                this.hideAll();
                this.moveToggleToHeader();
                this.updateToggleIcon('bars');
                if (this.toggleBtn) this.toggleBtn.style.display = '';
            } else {
                // Switched to desktop
                this.hideAll();
                document.body.style.overflow = '';
                if (this.toggleBtn) this.toggleBtn.style.display = 'none';
            }
        }
    }
    
    moveToggleToHeader() {
        const headerRight = document.querySelector('.content-header .ms-auto');
        if (this.toggleBtn && headerRight && this.toggleBtn.parentNode !== headerRight) {
            this.toggleBtn.classList.add('header-toggle');
            headerRight.appendChild(this.toggleBtn);
        }
    }

    moveToggleToSidebar() {
        if (this.toggleBtn && this.toggleBtn.parentNode !== this.sidebar) {
            this.sidebar.appendChild(this.toggleBtn);
        }
    }

    highlightActiveNav() {
        const currentPath = window.location.pathname.toLowerCase();
        const navLinks = this.sidebar.querySelectorAll('.nav-link');
        
        // Remove all active classes first
        navLinks.forEach(link => link.classList.remove('active'));
        
        // Find and highlight the active link
        let activeLink = null;
        let maxMatchLength = 0;
        
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && href !== '#') {
                const linkPath = href.toLowerCase();
                
                // Exact match or starts with match
                if (currentPath === linkPath || 
                    (currentPath.startsWith(linkPath) && linkPath.length > maxMatchLength)) {
                    maxMatchLength = linkPath.length;
                    activeLink = link;
                }
            }
        });
        
        if (activeLink) {
            activeLink.classList.add('active');
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new ModernSidebar();
});

// Re-initialize on page navigation (for SPA-like behavior)
window.addEventListener('popstate', () => {
    setTimeout(() => {
        const sidebar = new ModernSidebar();
    }, 100);
});


window.addEventListener("load", function () {
	const preloader = document.getElementById("preloader");
	const mainContent = document.querySelector(".main-content");

	// Ensure content fades in smoothly
	if (mainContent) {
		mainContent.classList.add("is-visible");
	}

	// Fade out preloader and remove after transition
	if (preloader) {
		preloader.classList.add("is-hidden");

		const removeAfterTransition = () => {
			if (preloader && preloader.parentNode) {
				preloader.parentNode.removeChild(preloader);
			}
		};

		// Use transitionend to remove after fade, with a safety timeout
		preloader.addEventListener("transitionend", removeAfterTransition, { once: true });
		setTimeout(removeAfterTransition, 800);
	}
});

// Extra safety: hide preloader on DOMContentLoaded in case load never fires
document.addEventListener("DOMContentLoaded", function () {
	const preloader = document.getElementById("preloader");
	const mainContent = document.querySelector(".main-content");

	if (preloader && !preloader.classList.contains("is-hidden")) {
		preloader.classList.add("is-hidden");
		preloader.addEventListener("transitionend", () => {
			if (preloader.parentNode) preloader.parentNode.removeChild(preloader);
		}, { once: true });
		setTimeout(() => {
			if (preloader && preloader.parentNode) preloader.parentNode.removeChild(preloader);
		}, 1500);
	}

	if (mainContent) mainContent.classList.add("is-visible");
});

// Absolute fallback: force remove after 5s regardless
setTimeout(() => {
	const preloader = document.getElementById("preloader");
	if (preloader && preloader.parentNode) preloader.parentNode.removeChild(preloader);
	const mainContent = document.querySelector(".main-content");
	if (mainContent) mainContent.classList.add("is-visible");
}, 5000);