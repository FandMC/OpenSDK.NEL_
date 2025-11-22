const getProtocol = () => {
  if (typeof window !== 'undefined' && window.location) {
    return window.location.protocol === 'https:' ? 'wss:' : 'ws:'
  }
  return 'ws:'
}
const getPort = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_PORT') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_NEL_PORT : null
  return fromStorage || fromEnv || null
}
const getHost = () => (typeof window !== 'undefined' && window.location && window.location.hostname) ? window.location.hostname : '127.0.0.1'
const getDefault = () => `${getHost()}:8080`
export const getWsUrl = () => {
  const proto = getProtocol()
  const port = getPort()
  const host = getHost()
  const authority = port ? `${host}:${port}` : getDefault()
  return `${proto}//${authority}/ws`
}
const getRandomNameUrl = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_RANDOM_NAME_URL') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_RANDOM_NAME_URL : null
  return fromStorage || fromEnv || '/nel-random-name'
}
const getAnnouncementUrl = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_ANNOUNCEMENT_URL') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_ANNOUNCEMENT_URL : null
  return fromStorage || fromEnv || '/nel-announcement'
}
export default { getWsUrl, getRandomNameUrl, getAnnouncementUrl }