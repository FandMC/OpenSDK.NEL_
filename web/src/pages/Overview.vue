<template>
  <div class="overview">
    <div class="card status-card">
      <div class="card-title">连接状态</div>
      <div class="card-body">
        <div class="row">
          连接状态: <span :class="['status', statusClass]">{{ statusText }}</span>
        </div>
        <div class="row ws">{{ wsUrl }}</div>
      </div>
    </div>

    <div class="card accounts">
      <div class="card-title">
        账号
        <button class="add-btn" @click="showAdd = true">添加账号</button>
      </div>
      <div class="card-body">
        <ul class="account-list">
          <li v-for="(acc, i) in accounts" :key="i" class="account-item">
            <div class="account-info">
              <div class="account-id">{{ acc.entityId || '未分配' }}</div>
              <div class="account-type">{{ acc.channel }}</div>
            </div>
            <button class="del-btn" @click="removeAccount(acc)">删除</button>
          </li>
          <li v-if="accounts.length === 0" class="empty">暂无账号</li>
        </ul>
      </div>
    </div>

    <Modal v-model="showAdd" title="添加账号">
      <div class="type-select">
        <button :class="['seg', newType === 'cookie' ? 'active' : '']" @click="newType = 'cookie'">Cookie</button>
        <button :class="['seg', newType === 'pc4399' ? 'active' : '']" @click="newType = 'pc4399'">PC4399</button>
        <button :class="['seg', newType === 'netease' ? 'active' : '']" @click="newType = 'netease'">网易邮箱</button>
      </div>
      <div class="form" v-if="newType === 'cookie'">
        <label>Cookie</label>
        <input v-model="cookieText" class="input" placeholder="填写Cookie" />
      </div>
      <div class="form" v-else-if="newType === 'pc4399'">
        <label>账号</label>
        <input v-model="pc4399Account" class="input" placeholder="填写账号" />
        <label>密码</label>
        <input v-model="pc4399Password" type="password" class="input" placeholder="填写密码" />
        <div class="free-row">
          <button class="btn btn-primary" :disabled="freeBusy" @click="getFreeAccount">获取账号</button>
          <div :class="['free-alert', freeLevel, { show: !!freeMessage }]">{{ freeMessage }}</div>
        </div>
      </div>
      <div class="form" v-else>
        <label>邮箱</label>
        <input v-model="neteaseEmail" class="input" placeholder="填写邮箱" />
        <label>密码</label>
        <input v-model="neteasePassword" type="password" class="input" placeholder="填写密码" />
      </div>
      <template #actions>
        <button class="btn" @click="confirmAdd">确定</button>
        <button class="btn secondary" @click="showAdd = false">取消</button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import Modal from '../components/Modal.vue'
import appConfig from '../config/app.js'
import { ref, onMounted, onUnmounted, computed } from 'vue'
const wsUrl = appConfig.getWsUrl()
const accounts = ref([])
const showAdd = ref(false)
const newType = ref('cookie')
const cookieText = ref('')
const pc4399Account = ref('')
const pc4399Password = ref('')
const neteaseEmail = ref('')
const neteasePassword = ref('')
const connected = ref(false)
const connecting = ref(true)
let socket
const freeMessage = ref('')
const freeBusy = ref(false)
const freeLevel = ref('')
function getFreeAccount() {
  if (!socket || socket.readyState !== 1) return
  freeBusy.value = true
  freeMessage.value = ''
  freeLevel.value = ''
  try { socket.send(JSON.stringify({ type: 'get_free_account' })) } catch {}
}
function confirmAdd() {
  if (!socket || socket.readyState !== 1) return
  if (newType.value === 'cookie') {
    const v = cookieText.value && cookieText.value.trim()
    if (!v) return
    try { socket.send(JSON.stringify({ type: 'cookie_login', cookie: v })) } catch {}
    cookieText.value = ''
  } else if (newType.value === 'pc4399') {
    const acc = pc4399Account.value && pc4399Account.value.trim()
    const pwd = pc4399Password.value && pc4399Password.value.trim()
    if (!acc || !pwd) return
    try { socket.send(JSON.stringify({ type: 'login_4399', account: acc, password: pwd })) } catch {}
    pc4399Account.value = ''
    pc4399Password.value = ''
  } else {
    const email = neteaseEmail.value && neteaseEmail.value.trim()
    const pwd = neteasePassword.value && neteasePassword.value.trim()
    if (!email || !pwd) return
    try { socket.send(JSON.stringify({ type: 'login_x19', email, password: pwd })) } catch {}
    neteaseEmail.value = ''
    neteasePassword.value = ''
  }
  
}

function removeAccount(acc) {
  if (!socket || socket.readyState !== 1) return
  if (!acc || !acc.entityId) return
  try { socket.send(JSON.stringify({ type: 'delete_account', entityId: acc.entityId })) } catch {}
}

const statusText = computed(() => {
  if (connecting.value) return '连接中'
  return connected.value ? '已连接' : '未连接'
})
const statusClass = computed(() => connected.value ? 'connected' : 'disconnected')

onMounted(() => {
  try {
    socket = new WebSocket(wsUrl)
    socket.onopen = () => {
      connected.value = true
      connecting.value = false
      try { socket.send(JSON.stringify({ type: 'list_accounts' })) } catch {}
    }
    socket.onclose = () => {
      connected.value = false
      connecting.value = false
    }
    socket.onerror = () => {
      connected.value = false
      connecting.value = false
    }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'accounts' && Array.isArray(msg.items)) {
        accounts.value = msg.items
      } else if (msg.type === 'Success_login') {
        if (msg.entityId && msg.channel) {
          const exists = accounts.value.some(a => a.entityId === msg.entityId)
          if (!exists) accounts.value.push({ entityId: msg.entityId, channel: msg.channel })
          showAdd.value = false
        }
      } else if (msg.type === 'get_free_account_status') {
        freeMessage.value = msg.message || '获取中...'
        freeLevel.value = 'info'
        freeBusy.value = true
      } else if (msg.type === 'get_free_account_result') {
        freeBusy.value = false
        if (msg.success) {
          pc4399Account.value = msg.account || ''
          pc4399Password.value = msg.password || ''
          freeMessage.value = '获取成功！已自动填充。'
          freeLevel.value = 'ok'
        } else {
          freeMessage.value = msg.message || '获取失败'
          freeLevel.value = 'error'
        }
      } else if (msg.type === 'login_error') {
      } else if (msg.type === 'connected') {
      } else if (msg.type === 'channels') {
      } else if (msg.type === 'captcha_required') {
      }
    }
  } catch {}
})
onUnmounted(() => {
  try {
    if (socket && socket.readyState === 1) socket.close()
  } catch {}
})
</script>

<style scoped>
.overview {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  height: 100%;
  align-items: flex-start;
  justify-content: flex-start;
}
.card {
  border: 1px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-background);
  color: var(--color-text);
}
.status-card {
  width: 360px;
}
.card-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  font-size: 16px;
  font-weight: 600;
  border-bottom: 1px solid var(--color-border);
}
.card-body {
  padding: 12px 16px;
}
.row {
  margin-bottom: 8px;
}
.status.connected {
  margin-left: 8px;
  color: #10b981;
}
.status.disconnected {
  margin-left: 8px;
  color: #ef4444;
}
.ws {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}
.accounts {
  width: 100%;
}
.accounts .add-btn {
  padding: 6px 10px;
  font-size: 14px;
  border: 1px solid var(--color-border);
  background: var(--color-background-soft);
  color: var(--color-text);
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 200ms ease, border-color 200ms ease, transform 100ms ease;
}
.accounts .add-btn:hover {
  background: var(--color-background-mute);
  border-color: var(--color-border-hover);
}
.accounts .add-btn:active {
  transform: scale(0.98);
}
.account-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: 1fr;
  gap: 8px;
}
.account-item {
  padding: 8px 10px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.account-id {
  font-size: 14px;
  font-weight: 600;
}
.account-type {
  font-size: 12px;
  opacity: 0.7;
}
.del-btn {
  padding: 6px 10px;
  border: 1px solid var(--color-border);
  background: var(--color-background);
  color: #ef4444;
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 200ms ease, border-color 200ms ease, transform 100ms ease;
}
.del-btn:hover {
  background: var(--color-background-mute);
  border-color: var(--color-border-hover);
}
.del-btn:active {
  transform: scale(0.98);
}
.empty {
  color: var(--color-text);
  opacity: 0.6;
}
.form {
  display: grid;
  gap: 8px;
}
.type-select {
  display: flex;
  gap: 8px;
  padding: 0 0 12px;
}
.seg {
  padding: 6px 10px;
  border: 1px solid var(--color-border);
  background: var(--color-background);
  color: var(--color-text);
  border-radius: 8px;
  cursor: pointer;
  transition: border-color 200ms ease, box-shadow 200ms ease, background-color 200ms ease;
}
.seg.active {
  background: var(--color-background-soft);
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2);
}
.input {
  padding: 8px 10px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  color: var(--color-text);
  transition: border-color 200ms ease, box-shadow 200ms ease;
}
.input:focus {
  outline: none;
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.25);
}
.btn {
  padding: 8px 12px;
  border: 1px solid var(--color-border);
  background: var(--color-background);
  color: var(--color-text);
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 200ms ease, border-color 200ms ease, transform 100ms ease;
}
.btn:hover {
  background: var(--color-background-mute);
  border-color: var(--color-border-hover);
}
.btn:active {
  transform: scale(0.98);
}
.btn.secondary {
  background: var(--color-background-soft);
}

.btn.btn-primary {
  border-color: #10b981;
  box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
}
.free-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
}
.free-alert {
  display: none;
  font-size: 12px;
}
.free-alert.show { display: block; }
.free-alert.info { color: #1e90ff; }
.free-alert.ok { color: #10b981; }
.free-alert.error { color: #ef4444; }
</style>