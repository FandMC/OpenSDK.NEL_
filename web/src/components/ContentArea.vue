<template>
  <div class="content-wrapper">
    <div class="content-body">
      <component :is="currentComponent" />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { pages } from '../pages/index.js'
const props = defineProps({ currentPage: String })
const currentComponent = computed(() => pages[props.currentPage])
</script>

<style scoped>
.content-wrapper {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.content-body {
  flex: 1;
  display: flex;
  align-items: flex-end;
  justify-content: flex-end;
  padding: 16px;
}
</style>