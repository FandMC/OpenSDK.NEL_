<template>
  <div class="game-page">
    <div class="header">
      <div class="title">游戏</div>
    </div>
    <div v-if="channels.length === 0" class="empty-top">暂无通道</div>
    <div class="list">
      <div v-for="ch in channels" :key="ch.identifier" class="row">
        <div class="info">
          <div class="server">{{ ch.serverName }}</div>
          <div class="player">{{ ch.playerId }}</div>
          <div class="ip">{{ ch.tcp }}</div>
        </div>
        <div class="actions">
          <button class="btn" @click="copyIp(ch.tcp)">复制IP</button>
          <button class="btn danger" @click="shutdown(ch.identifier)">关闭通道</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import appConfig from '../config/app.js'
const channels = ref([])
let socket
function requestChannels() {
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'list_channels' })) } catch {}
}
function copyIp(text) {
  if (!text) return
  if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(text).catch(() => {})
  }
}
function shutdown(identifier) {
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'shutdown_game', identifiers: [identifier] })) } catch {}
}
onMounted(() => {
  try {
    socket = new WebSocket(appConfig.getWsUrl())
    socket.onopen = () => { requestChannels() }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'channels' && Array.isArray(msg.items)) {
        channels.value = (msg.items || []).map(it => ({
          serverName: it.serverName,
          playerId: it.playerId,
          tcp: it.tcp,
          identifier: it.identifier
        }))
      } else if (msg.type === 'channels_updated' || msg.type === 'shutdown_ack') {
        requestChannels()
      }
    }
  } catch {}
})
onUnmounted(() => { try { if (socket && socket.readyState === 1) socket.close() } catch {} })
</script>

<style scoped>
.game-page { display: flex; flex-direction: column; gap: 12px; width: 100%; align-self: flex-start; margin-right: auto; }
.header { display: flex; align-items: center; justify-content: space-between; }
.title { font-size: 16px; font-weight: 600; }
.empty-top { padding: 10px 12px; opacity: 0.7; }
.list { display: grid; grid-template-columns: 1fr; gap: 8px; }
.row { display: flex; align-items: center; justify-content: space-between; border: 1px solid var(--color-border); border-radius: 12px; padding: 12px; background: var(--color-background); color: var(--color-text); }
.server { font-size: 14px; font-weight: 600; }
.player { font-size: 12px; opacity: 0.7; }
.ip { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace; font-size: 12px; }
.actions { display: flex; gap: 8px; }
.btn { padding: 8px 12px; border: 1px solid var(--color-border); background: var(--color-background); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: background-color 200ms ease, border-color 200ms ease, transform 100ms ease; }
.btn:hover { background: var(--color-background-mute); border-color: var(--color-border-hover); }
.btn:active { transform: scale(0.98); }
.btn.danger { color: #ef4444; }
</style>