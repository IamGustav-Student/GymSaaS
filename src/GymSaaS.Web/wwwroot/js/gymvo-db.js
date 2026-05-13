/**
 * GYMVO DB - Capa de Persistencia Local (IndexedDB)
 * Utiliza Dexie.js para manejar los datos del gimnasio de forma offline.
 */

// Importante: Asegurarse de que Dexie esté cargado antes de este script
const db = new Dexie("GymvoOfflineDB");

// Definición del esquema
// Versión 1: Socios y Asistencias Pendientes
db.version(1).stores({
    socios: "++id, dni, qrCode, nombre, estadoMembresia",
    asistencias: "++id, socioId, fechaHora, sincronizado"
});

const GymvoDB = {
    // --- SOCIOS ---
    async saveSocios(sociosList) {
        try {
            await db.socios.clear();
            return await db.socios.bulkAdd(sociosList);
        } catch (error) {
            console.error("Error guardando socios localmente:", error);
        }
    },

    async findSocioByQR(qrCode) {
        return await db.socios.where("qrCode").equals(qrCode).first();
    },

    async getSocioCount() {
        return await db.socios.count();
    },

    // --- ASISTENCIAS ---
    async queueAsistencia(socioId) {
        const asistencia = {
            socioId: socioId,
            fechaHora: new Date().toISOString(),
            sincronizado: 0
        };
        return await db.asistencias.add(asistencia);
    },

    async getAsistenciasPendientes() {
        return await db.asistencias.where("sincronizado").equals(0).toArray();
    },

    async markAsSincronizada(id) {
        return await db.asistencias.update(id, { sincronizado: 1 });
    },

    async clearSincronizadas() {
        return await db.asistencias.where("sincronizado").equals(1).delete();
    }
};

window.GymvoDB = GymvoDB;
console.log("⚡ GymvoDB Inicializado localmente");
