<template>
  <div class="dropdown" @click="toggle">
    <div class="selected">
      <div class="text">{{ currentLabel }}</div>
      <div class="arrow" :class="{ open: open }">^</div>
    </div>
    <div class="options" :class="{ open: open }" @click.stop>
      <div v-if="items.length === 0" class="option tip">空</div>
      <div
        v-for="it in items"
        :key="it.value"
        class="option"
        :class="{ active: it.value === modelValue }"
        @click="choose(it.value)"
      >
        <div class="opt-label">{{ it.label }}</div>
        <div v-if="it.description" class="opt-desc">{{ it.description }}</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { defineProps, defineEmits, ref, computed } from 'vue'
const props = defineProps({
  modelValue: [String, Number],
  items: { type: Array, default: () => [] },
  placeholder: { type: String, default: '空' }
})
const emit = defineEmits(['update:modelValue'])
const open = ref(false)
const currentLabel = computed(() => {
  const found = props.items.find(i => i.value === props.modelValue)
  return found ? (found.description ? `${found.label}（${found.description}）` : found.label) : props.placeholder
})
function toggle() { open.value = !open.value }
function choose(v) { emit('update:modelValue', v); open.value = false }
</script>

<style scoped>
.dropdown { position: relative; }
.selected { display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 8px 10px; border: 1px solid var(--color-border); border-radius: 8px; background: var(--color-background); color: var(--color-text); cursor: pointer; }
.text { flex: 1; }
.arrow { font-size: 14px; line-height: 1; transition: transform 200ms ease; }
.arrow.open { transform: rotate(180deg); }
.options { position: absolute; z-index: 10; margin-top: 6px; width: 100%; max-height: 0; overflow: hidden; border: 1px solid transparent; border-radius: 8px; background: var(--color-background); box-shadow: 0 10px 30px rgba(0,0,0,0.2); opacity: 0; transform-origin: top; transform: scaleY(0.98); transition: max-height 220ms ease, opacity 180ms ease, transform 220ms ease, border-color 220ms ease; }
.options.open { max-height: 240px; opacity: 1; transform: scaleY(1); border-color: var(--color-border); }
.option { padding: 8px 10px; border-bottom: 1px solid var(--color-border); cursor: pointer; }
.option:last-child { border-bottom: none; }
.option:hover { background: var(--color-background-mute); }
.option.active { border-left: 4px solid #10b981; }
.opt-label { font-size: 13px; font-weight: 600; }
.opt-desc { font-size: 12px; opacity: 0.7; }
.tip { opacity: 0.7; cursor: default; }
</style>