// Yoklama System - Sidebar Only

// Conflict Modal Handler
class ConflictModal {
    constructor() {
        this.modal = null;
        this.init();
    }

    init() {
        this.createModal();
        this.checkForConflictMessages();
    }

    createModal() {
        if (document.getElementById('conflictModal')) return;

        this.modal = document.createElement('div');
        this.modal.id = 'conflictModal';
        this.modal.className = 'modal fade';
        this.modal.setAttribute('tabindex', '-1');
        this.modal.setAttribute('aria-hidden', 'true');
        this.modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content border-0 shadow-lg">
                    <div class="modal-header text-white" style="background: linear-gradient(90deg, #e03131, #ff8787);">
                        <h5 class="modal-title">
                            <i class="fas fa-exclamation-triangle me-2"></i>Çakışma Uyarısı
                        </h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body py-4">
                        <div class="d-flex align-items-start">
                            <div class="me-3">
                                <span class="badge bg-warning text-dark text-uppercase">Dikkat</span>
                            </div>
                            <div class="flex-fill" id="conflictMessage">
                                <!-- Conflict message will be inserted here -->
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer bg-light-subtle">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Tamam</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(this.modal);
    }

    checkForConflictMessages() {
        // Check for TempData conflict messages
        const conflictMessage = document.querySelector('[data-conflict-message]');
        if (conflictMessage) {
            const message = conflictMessage.getAttribute('data-conflict-message');
            this.show(message);
        }

        // Check for TempData via server-side rendering
        const tempDataConflict = document.querySelector('#temp-conflict-error');
        if (tempDataConflict) {
            const message = tempDataConflict.textContent;
            this.show(message);
        }
    }

    show(message) {
        if (!this.modal) return;

        const messageElement = this.modal.querySelector('#conflictMessage');
        if (messageElement) {
            messageElement.textContent = message;
        }

        const bsModal = new bootstrap.Modal(this.modal);
        bsModal.show();
    }

    static showConflict(message) {
        const modal = new ConflictModal();
        modal.show(message);
    }
}

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
        
        // Event handlers
        this.toggleSidebarHandler = null;
        this.toggleAccountSidebarHandler = null;
        
        console.log('ModernSidebar initialized');
        console.log('Sidebar element:', this.sidebar);
        console.log('Account sidebar element:', this.accountSidebar);
        console.log('Account toggle element:', this.accountToggle);
        console.log('Is mobile/tablet:', this.isMobileOrTablet);
        
        this.init();
    }

    init() {
        if (!this.sidebar) {
            console.log('Sidebar not found!');
            return;
        }
        
        this.createToggleButton();
        this.createMobileOverlay();
        this.bindEvents();
        this.setInitialState();
        this.highlightActiveNav();
    }

    createToggleButton() {
        // Find existing toggle button in layout by ID first, then by class
        this.toggleBtn = document.getElementById('sidebarToggleBtn') || document.querySelector('.sidebar-toggle');
        
        console.log('Looking for toggle button...');
        console.log('Found by ID:', document.getElementById('sidebarToggleBtn'));
        console.log('Found by class:', document.querySelector('.sidebar-toggle'));
        console.log('Final toggle button:', this.toggleBtn);
        
        // If no toggle button exists, create one
        if (!this.toggleBtn) {
            const toggleBtn = document.createElement('button');
            toggleBtn.className = 'sidebar-toggle btn btn-icon btn-sidebar rounded-circle';
            toggleBtn.innerHTML = '<i class="fas fa-bars-staggered"></i>';
            toggleBtn.setAttribute('aria-label', 'Menüyü Aç/Kapat');
            toggleBtn.type = 'button';
            
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
        // Check if overlay already exists
        this.mobileOverlay = document.querySelector('.mobile-overlay');
        
        if (!this.mobileOverlay) {
            this.mobileOverlay = document.createElement('div');
            this.mobileOverlay.className = 'mobile-overlay d-md-none';
            this.mobileOverlay.addEventListener('click', () => {
                this.hideAll();
            });
            document.body.appendChild(this.mobileOverlay);
        }
    }

    bindEvents() {
        // Toggle button click
        if (this.toggleBtn && !this.toggleBtn.hasAttribute('data-listener-added')) {
            console.log('Toggle button found, adding event listener');
            this.toggleBtn.setAttribute('data-listener-added', 'true');
            this.toggleBtn.addEventListener('click', (e) => {
                console.log('Toggle button clicked!');
                e.preventDefault();
                e.stopPropagation();
                this.toggleSidebar();
            });
        } else if (this.toggleBtn) {
            console.log('Toggle button already has event listener');
        } else {
            console.log('Toggle button NOT found!');
        }

        // Account toggle button click
        if (this.accountToggle && !this.accountToggle.hasAttribute('data-listener-added')) {
            console.log('Account toggle button found, adding event listener');
            this.accountToggle.setAttribute('data-listener-added', 'true');
            this.accountToggle.addEventListener('click', (e) => {
                console.log('Account toggle button clicked!');
                e.preventDefault();
                e.stopPropagation();
                this.toggleAccountSidebar();
            });
        } else if (this.accountToggle) {
            console.log('Account toggle button already has event listener');
        } else {
            console.log('Account toggle button NOT found!');
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
            this.updateToggleIcon('bars-staggered');
            if (this.toggleBtn) this.toggleBtn.style.display = '';
        } else {
            // Desktop: always visible, no toggle behavior
            this.sidebar.classList.remove('show', 'collapsed');
            if (this.toggleBtn) this.toggleBtn.style.display = 'none';
        }
        
        // Ensure account sidebar starts hidden
        if (this.accountSidebar) {
            this.accountSidebar.classList.remove('show');
        }
    }

    toggleSidebar() {
        console.log('toggleSidebar called');
        console.log('isMobileOrTablet:', this.isMobileOrTablet);
        
        if (!this.isMobileOrTablet) {
            console.log('Not mobile/tablet, returning');
            return; // no toggle on desktop
        }
        
        console.log('Sidebar has show class:', this.sidebar.classList.contains('show'));
        
        if (this.sidebar.classList.contains('show')) {
            console.log('Hiding sidebar');
            this.hideSidebar();
        } else {
            console.log('Showing sidebar');
            this.showSidebar();
        }
    }

    toggleAccountSidebar() {
        if (!this.isMobileOrTablet) {
            return; // no toggle on desktop
        }
        
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
        console.log('showSidebar called');
        if (!this.isMobileOrTablet) {
            console.log('Not mobile/tablet in showSidebar, returning');
            return;
        }
        
        // Hide account sidebar first
        this.hideAccountSidebar();
        
        console.log('Adding show class to sidebar');
        this.sidebar.classList.add('show');
        console.log('Sidebar classes after adding show:', this.sidebar.className);
        
        this.updateToggleIcon('times');
        this.showOverlay();
        document.body.style.overflow = 'hidden';
        
        console.log('showSidebar completed');
    }

    hideSidebar() {
        console.log('hideSidebar called');
        if (!this.isMobileOrTablet) {
            console.log('Not mobile/tablet in hideSidebar, returning');
            return;
        }
        
        console.log('Removing show class from sidebar');
        this.sidebar.classList.remove('show');
        console.log('Sidebar classes after removing show:', this.sidebar.className);
        
        this.updateToggleIcon('bars-staggered');
        this.hideOverlay();
        document.body.style.overflow = '';
        
        console.log('hideSidebar completed');
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
            // Use bars-staggered for open state, times for close state
            const icon = iconName === 'times' ? 'fa-times' : 'fa-bars-staggered';
            this.toggleBtn.innerHTML = `<i class="fas ${icon}"></i>`;
            if (iconName === 'times') {
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
                this.updateToggleIcon('bars-staggered');
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

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.modernSidebar = new ModernSidebar();
    window.conflictModal = new ConflictModal();
});