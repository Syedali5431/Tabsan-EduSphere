// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
	function formatTime(value) {
		if (!value) {
			return '';
		}

		var date = new Date(value);
		if (Number.isNaN(date.getTime())) {
			return '';
		}

		return new Intl.DateTimeFormat(undefined, {
			hour: 'numeric',
			minute: '2-digit'
		}).format(date);
	}

	function escapeHtml(value) {
		return String(value ?? '')
			.replace(/&/g, '&amp;')
			.replace(/</g, '&lt;')
			.replace(/>/g, '&gt;')
			.replace(/"/g, '&quot;')
			.replace(/'/g, '&#39;');
	}

	function createMessageMarkup(message) {
		var role = (message.role || 'assistant').toLowerCase() === 'user' ? 'user' : 'assistant';
		var content = escapeHtml(message.content || '');
		var sentAt = formatTime(message.createdAt || message.sentAt);

		return '' +
			'<article class="ai-chat-message is-' + role + '">' +
				'<div class="ai-chat-message-bubble">' +
					'<p class="ai-chat-message-content">' + content + '</p>' +
					'<div class="ai-chat-message-meta">' + sentAt + '</div>' +
				'</div>' +
			'</article>';
	}

	function createTypingMarkup() {
		return '' +
			'<article class="ai-chat-message is-assistant is-typing" data-ai-chat-typing>' +
				'<div class="ai-chat-message-bubble">' +
					'<span class="ai-chat-typing" aria-label="Assistant is typing">' +
						'<span></span><span></span><span></span>' +
					'</span>' +
				'</div>' +
			'</article>';
	}

	function createEmptyMarkup() {
		return '' +
			'<div class="ai-chat-empty">' +
				'<h3 class="ai-chat-empty-title">Start a conversation</h3>' +
				'<p class="ai-chat-empty-copy">Ask about schedules, assignments, results, degree planning, or where to find a portal feature.</p>' +
			'</div>';
	}

	function createConversationMarkup(conversation, activeConversationId) {
		var isActive = activeConversationId && conversation.id === activeConversationId;
		var stamp = formatTime(conversation.lastMessageAt || conversation.createdAt);

		return '' +
			'<button type="button" class="ai-chat-conversation-item' + (isActive ? ' is-active' : '') + '" data-ai-chat-conversation-id="' + escapeHtml(conversation.id || '') + '">' +
				'<span class="ai-chat-conversation-title">' + escapeHtml(conversation.title || 'Conversation') + '</span>' +
				'<span class="ai-chat-conversation-meta">' + escapeHtml(stamp || 'New') + '</span>' +
			'</button>';
	}

	function createToastMarkup(message, type) {
		var variant = type || 'info';
		return '' +
			'<div class="ui-toast is-' + escapeHtml(variant) + '">' +
				'<div class="ui-toast-accent"></div>' +
				'<div class="ui-toast-body">' + escapeHtml(message) + '</div>' +
			'</div>';
	}

	document.addEventListener('DOMContentLoaded', function () {
		var loader = document.querySelector('[data-page-loader]');
		if (loader) {
			window.requestAnimationFrame(function () {
				loader.classList.add('is-hidden');
			});
		}

		var toastStack = document.querySelector('[data-toast-stack]');
		if (toastStack) {
			document.querySelectorAll('.alert, .validation-summary-errors, .field-validation-error').forEach(function (node) {
				var text = (node.textContent || '').trim();
				if (!text) {
					return;
				}

				var type = 'info';
				if (node.classList.contains('alert-danger') || node.classList.contains('validation-summary-errors') || node.classList.contains('field-validation-error')) {
					type = 'danger';
				} else if (node.classList.contains('alert-success')) {
					type = 'success';
				} else if (node.classList.contains('alert-warning')) {
					type = 'warning';
				}

				toastStack.insertAdjacentHTML('beforeend', createToastMarkup(text, type));
				if (!node.classList.contains('ui-toast-source')) {
					node.classList.add('visually-hidden');
				}
			});

			window.setTimeout(function () {
				toastStack.querySelectorAll('.ui-toast').forEach(function (toast) {
					toast.classList.add('is-visible');
				});
			}, 50);
		}

		var widget = document.querySelector('[data-ai-chat-widget]');
		if (!widget) {
			return;
		}

		var storageKey = 'edusphere.ai.activeConversationId';
		var toggle = widget.querySelector('[data-ai-chat-toggle]');
		var closeButton = widget.querySelector('[data-ai-chat-close]');
		var resetButton = widget.querySelector('[data-ai-chat-reset]');
		var backdrop = widget.querySelector('[data-ai-chat-backdrop]');
		var form = widget.querySelector('[data-ai-chat-form]');
		var input = widget.querySelector('[data-ai-chat-input]');
		var submit = widget.querySelector('[data-ai-chat-submit]');
		var messages = widget.querySelector('[data-ai-chat-messages]');
		var status = widget.querySelector('[data-ai-chat-status]');
		var conversations = widget.querySelector('[data-ai-chat-conversations]');
		var conversationCount = widget.querySelector('[data-ai-chat-conversation-count]');
		var threadContext = widget.querySelector('[data-ai-chat-thread-context]');
		var token = form.querySelector('input[name="__RequestVerificationToken"]');
		var state = {
			activeConversationId: sessionStorage.getItem(storageKey) || '',
			initialized: false,
			isOpen: false,
			isSending: false,
			items: [],
			conversations: []
		};

		function setStatus(text) {
			if (!text) {
				status.hidden = true;
				status.textContent = '';
				return;
			}

			status.hidden = false;
			status.textContent = text;
		}

		function persistConversation() {
			if (state.activeConversationId) {
				sessionStorage.setItem(storageKey, state.activeConversationId);
			} else {
				sessionStorage.removeItem(storageKey);
			}
		}

		function scrollMessagesToBottom() {
			messages.scrollTop = messages.scrollHeight;
		}

		function renderMessages() {
			if (!state.items.length) {
				messages.innerHTML = createEmptyMarkup();
				return;
			}

			messages.innerHTML = state.items.map(createMessageMarkup).join('');
			scrollMessagesToBottom();
		}

		function renderConversations() {
			conversationCount.textContent = String(state.conversations.length);

			if (!state.conversations.length) {
				conversations.innerHTML = '<div class="ai-chat-conversation-empty">No recent chats yet.</div>';
				return;
			}

			conversations.innerHTML = state.conversations
				.map(function (conversation) {
					return createConversationMarkup(conversation, state.activeConversationId);
				})
				.join('');
		}

		function renderThreadContext() {
			if (!state.activeConversationId) {
				threadContext.textContent = 'Ready for a new conversation';
				return;
			}

			var activeConversation = state.conversations.find(function (conversation) {
				return conversation.id === state.activeConversationId;
			});

			threadContext.textContent = activeConversation
				? (activeConversation.title || 'Current conversation')
				: 'Current conversation';
		}

		function appendTyping() {
			messages.insertAdjacentHTML('beforeend', createTypingMarkup());
			scrollMessagesToBottom();
		}

		function clearTyping() {
			var typing = messages.querySelector('[data-ai-chat-typing]');
			if (typing) {
				typing.remove();
			}
		}

		function setOpen(open) {
			state.isOpen = open;
			widget.classList.toggle('is-open', open);
			toggle.setAttribute('aria-expanded', open ? 'true' : 'false');

			if (open) {
				window.setTimeout(function () {
					input.focus();
				}, 120);
			}
		}

		function loadConversationState() {
			var url = widget.dataset.loadUrl;
			if (state.activeConversationId) {
				url += '?conversationId=' + encodeURIComponent(state.activeConversationId);
			}

			setStatus('Loading assistant history...');

			return fetch(url, {
				credentials: 'same-origin',
				headers: {
					'X-Requested-With': 'XMLHttpRequest'
				}
			})
				.then(function (response) {
					if (!response.ok) {
						throw new Error('Unable to load AI assistant state.');
					}

					return response.json();
				})
				.then(function (payload) {
					state.initialized = true;
					state.activeConversationId = payload.activeConversationId || '';
					state.items = Array.isArray(payload.messages) ? payload.messages : [];
					state.conversations = Array.isArray(payload.conversations) ? payload.conversations : [];
					persistConversation();
					renderConversations();
					renderThreadContext();
					renderMessages();
					setStatus(payload.message || '');
				})
				.catch(function (error) {
					state.initialized = true;
					state.items = [];
					state.conversations = [];
					renderConversations();
					renderThreadContext();
					renderMessages();
					setStatus(error.message || 'Unable to load AI assistant state.');
				});
		}

		function loadSpecificConversation(conversationId) {
			state.activeConversationId = conversationId || '';
			persistConversation();
			state.items = [];
			renderThreadContext();
			renderMessages();
			renderConversations();
			state.initialized = false;
			return loadConversationState();
		}

		function upsertConversation(conversationId) {
			if (!conversationId) {
				return;
			}

			var existingConversation = state.conversations.find(function (conversation) {
				return conversation.id === conversationId;
			});

			if (!existingConversation) {
				state.conversations.unshift({
					id: conversationId,
					title: 'AI Chat · Today',
					createdAt: new Date().toISOString(),
					lastMessageAt: new Date().toISOString()
				});
				return;
			}

			existingConversation.lastMessageAt = new Date().toISOString();
			state.conversations = state.conversations.filter(function (conversation) {
				return conversation.id !== conversationId;
			});
			state.conversations.unshift(existingConversation);
		}

		function ensureInitialized() {
			if (state.initialized) {
				return Promise.resolve();
			}

			return loadConversationState();
		}

		function sendMessage(messageText) {
			state.isSending = true;
			submit.disabled = true;
			setStatus('');

			var optimisticMessage = {
				role: 'user',
				content: messageText,
				createdAt: new Date().toISOString()
			};

			state.items.push(optimisticMessage);
			renderMessages();
			appendTyping();

			var formData = new FormData();
			formData.append('__RequestVerificationToken', token ? token.value : '');
			formData.append('message', messageText);
			if (state.activeConversationId) {
				formData.append('conversationId', state.activeConversationId);
			}

			return fetch(widget.dataset.sendUrl, {
				method: 'POST',
				body: formData,
				credentials: 'same-origin',
				headers: {
					'X-Requested-With': 'XMLHttpRequest'
				}
			})
				.then(function (response) {
					if (!response.ok) {
						throw new Error('Unable to send message.');
					}

					return response.json();
				})
				.then(function (payload) {
					if (!payload.success) {
						throw new Error(payload.error || 'AI assistant is currently unavailable.');
					}

					state.activeConversationId = payload.conversationId || state.activeConversationId;
					persistConversation();
					upsertConversation(state.activeConversationId);
					state.items.push(payload.assistantMessage || {
						role: 'assistant',
						content: 'No response returned.',
						createdAt: new Date().toISOString()
					});
					renderConversations();
					renderThreadContext();
					renderMessages();
				})
				.catch(function (error) {
					state.items.pop();
					renderMessages();
					input.value = messageText;
					setStatus(error.message || 'Unable to send message.');
				})
				.finally(function () {
					state.isSending = false;
					submit.disabled = false;
					clearTyping();
					input.focus();
				});
		}

		toggle.addEventListener('click', function (event) {
			event.preventDefault();

			var shouldOpen = !state.isOpen;
			setOpen(shouldOpen);

			if (shouldOpen) {
				ensureInitialized();
			}
		});

		closeButton.addEventListener('click', function () {
			setOpen(false);
		});

		backdrop.addEventListener('click', function () {
			setOpen(false);
		});

		resetButton.addEventListener('click', function () {
			state.activeConversationId = '';
			state.items = [];
			persistConversation();
			renderConversations();
			renderThreadContext();
			renderMessages();
			setStatus('New conversation ready.');
			input.focus();
		});

		conversations.addEventListener('click', function (event) {
			var button = event.target.closest('[data-ai-chat-conversation-id]');
			if (!button) {
				return;
			}

			var conversationId = button.getAttribute('data-ai-chat-conversation-id') || '';
			if (!conversationId || conversationId === state.activeConversationId || state.isSending) {
				return;
			}

			loadSpecificConversation(conversationId);
		});

		form.addEventListener('submit', function (event) {
			event.preventDefault();
			if (state.isSending) {
				return;
			}

			var messageText = (input.value || '').trim();
			if (!messageText) {
				input.focus();
				return;
			}

			input.value = '';
			sendMessage(messageText);
		});

		document.addEventListener('keydown', function (event) {
			if (event.key === 'Escape' && state.isOpen) {
				setOpen(false);
			}
		});

		renderConversations();
		renderThreadContext();
		renderMessages();
	});
})();
