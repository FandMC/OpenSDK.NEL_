<template>
  <nav class="menu">
    <div class="menu-header">菜单</div>
    <ul class="menu-list" ref="listRef">
      <div class="indicator" :style="indicatorStyle"></div>
      <li
        v-for="(item, idx) in items"
        :key="item.key"
        class="menu-item"
        :class="{ active: item.key === modelValue }"
        :ref="el => itemRefs[idx] = el"
        @click="$emit('update:modelValue', item.key)"
      >
        {{ item.label }}
      </li>
    </ul>
  </nav>
</template>

<script setup>
import { defineProps, defineEmits, ref, onMounted, watch, nextTick, computed } from 'vue'
const props = defineProps({
  modelValue: String,
  items: Array
})
defineEmits(['update:modelValue'])

const listRef = ref(null)
const itemRefs = ref([])
const y = ref(0)
const h = ref(0)
const indicatorStyle = computed(() => ({
  transform: `translateY(${y.value + 3}px)`,
  height: `${Math.max(0, h.value - 6)}px`
}))

function updateIndicator() {
  nextTick(() => {
    const idx = props.items.findIndex(i => i.key === props.modelValue)
    const el = itemRefs.value[idx]
    if (!el || !listRef.value) return
    const top = el.offsetTop
    const height = el.offsetHeight
    y.value = top
    h.value = height
  })
}

onMounted(updateIndicator)
watch(() => props.modelValue, updateIndicator)
</script>

<style scoped>
.menu {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.menu-header {
  padding: 16px;
  font-size: 16px;
  font-weight: 600;
  color: #ffffff;
}
.menu-list {
  list-style: none;
  margin: 0;
  padding: 0 8px;
  position: relative;
}
.menu-item {
  position: relative;
  padding: 10px 8px 10px 14px;
  border-radius: 6px;
  cursor: pointer;
}
.menu-item:hover {
  background: var(--color-background-soft);
}
.indicator {
  position: absolute;
  left: 10px;
  width: 4px;
  background: #10b981;
  border-radius: 2px;
  z-index: 1;
  transition: transform 200ms ease, height 200ms ease;
}
</style>