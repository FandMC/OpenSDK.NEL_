<script setup>
import { ref } from 'vue'
import Sidebar from './components/Sidebar.vue'
import ContentArea from './components/ContentArea.vue'
import menuItems from './config/menu.js'
const currentPage = ref(menuItems[0].key)
</script>

<template>
  <div class="layout">
    <aside class="sidebar">
      <Sidebar v-model="currentPage" :items="menuItems" />
    </aside>
    <main class="content">
      <ContentArea :currentPage="currentPage" />
    </main>
  </div>
</template>
