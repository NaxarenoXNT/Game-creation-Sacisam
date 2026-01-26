# ⭐ Guía de Adaptación en Unity

Esta guía detalla todos los cambios que debes realizar en el Editor de Unity después de las modificaciones del código.

---

## 1. Verificar Compilación

Antes de hacer cualquier cambio, asegúrate de que Unity compile sin errores:
1. Abre Unity y espera a que termine de importar
2. Revisa la consola (Window > General > Console)
3. Si hay errores, revisa que todos los archivos .cs estén en su lugar

---

## 2. Crear GameConfig (Singleton de Configuración)

### Paso a paso:
1. **Crear el asset**:
   - Click derecho en `Assets/Resources` > Create > Configuracion > Game Config
   - Nombrarlo `GameConfig`

2. **Configurar elementos**:
   - En el Inspector, añadir ElementDefinition para cada elemento:
     - Fire, Water, Earth, Wind, Light, Dark, etc.
   - Asignar los sprites de iconos si los tienes

3. **El GameConfig debe estar en Resources** para que el singleton lo encuentre automáticamente

---

## 3. Crear ClaseData para Jugadores

### Crear un Guerrero:
1. Click derecho en carpeta de tu elección > Create > Combate > Clase Data
2. Configurar:
   ```
   Nombre: "Guerrero"
   Tipo Clase: Guerrero (enum)
   Vida Base: 120
   Ataque Base: 15
   Defensa Base: 8
   Mana Base: 40
   Velocidad Base: 10
   Atributos: None (o el elemento que quieras)
   Tipo Entidad: Jugador
   Estilo Combate: Melee
   ```

### Escalado (EscaladoJugador):
El escalado ahora está integrado en la clase. Los valores por defecto son:
- Vida por nivel: +12
- Ataque por nivel: +3
- Defensa por nivel: +2
- Mana por nivel: +5
- Velocidad por nivel: +1

---

## 4. Crear EnemigoData

### Crear un Goblin:
1. Click derecho > Create > Combate > Enemigo Data
2. Configurar:
   ```
   Nombre: "Goblin"
   Tipo Enemigo: Goblin (enum)
   Nivel Base: 1
   Vida Base: 50
   Ataque Base: 8
   Defensa Base: 3
   Velocidad Base: 12
   XP Otorgada: 25
   Oro Otorgado: 10
   Atributos: None
   Tipo Entidad: Humanoide
   Estilo Combate: Melee
   Habilidad Por Defecto: (asignar HabilidadData de ataque básico)
   ```

### Crear un Dragon:
```
Nombre: "Dragon Ancestral"
Tipo Enemigo: Dragon
Nivel Base: 10
Vida Base: 500
Ataque Base: 45
Defensa Base: 25
Velocidad Base: 8
XP Otorgada: 500
Oro Otorgado: 200
Atributos: Fire
Tipo Entidad: Dragon
Estilo Combate: Ranged
```

---

## 5. Crear HabilidadData

### Habilidad de Ataque Básico:
1. Click derecho > Create > Combate > Habilidad Data
2. Configurar:
   ```
   Nombre Habilidad: "Ataque"
   Descripcion: "Un golpe básico"
   Coste Mana: 0
   Cooldown Turnos: 0
   Tipo Objetivo: EnemigoUnico
   ```
3. En la lista de Efectos, añadir un DamageEffect:
   - Base Damage: 0 (usará el ATK del personaje)
   - Tipo Dano: None (físico)

### Habilidad de Fuego:
```
Nombre Habilidad: "Bola de Fuego"
Descripcion: "Lanza una bola de fuego"
Coste Mana: 15
Cooldown Turnos: 2
Tipo Objetivo: EnemigoUnico
Efectos:
  - DamageEffect: baseDamage=20, tipoDano=Fire
```

### Habilidad de Curación:
```
Nombre Habilidad: "Curar"
Descripcion: "Restaura vida"
Coste Mana: 10
Cooldown Turnos: 3
Tipo Objetivo: AliadoUnico
Efectos:
  - HealEffect: healAmount=30, porcentajeVidaMax=false
```

### Habilidad con Estado:
```
Nombre Habilidad: "Veneno"
Descripcion: "Envenena al objetivo"
Coste Mana: 8
Cooldown Turnos: 4
Tipo Objetivo: EnemigoUnico
Efectos:
  - StatusEffect: 
      statusAplicar=Poisoned
      duracionTurnos=3
      danoPorTurno=5
      modificadorStats=0
```

---

## 6. Configurar EntityController (Jugadores)

### En cada GameObject de jugador:
1. Añadir componente `EntityController`
2. Añadir componente `EntityStats` (se crea automáticamente si falta)
3. Configurar en el Inspector:
   ```
   Datos Clase: (arrastrar ClaseData del Guerrero)
   Habilidad Por Defecto: (arrastrar HabilidadData de ataque)
   Habilidades Disponibles: (lista de HabilidadData)
   Entity Stats: (se auto-asigna)
   ```

---

## 7. Configurar EnemyController (Enemigos)

### En cada GameObject de enemigo:
1. Añadir componente `EnemyController`
2. Añadir componente `EntityStats`
3. Configurar:
   ```
   Datos Enemigo: (arrastrar EnemigoData del Goblin)
   Entity Stats: (se auto-asigna)
   ```

---

## 8. Configurar CombateManager

### En un GameObject vacío llamado "CombateManager":
1. Añadir componente `CombateManager`
2. Configurar listas:
   ```
   Jugadores Controllers: (arrastrar los EntityController de jugadores)
   Enemigos Controllers: (arrastrar los EnemyController de enemigos)
   ```

---

## 9. Configurar ObjectPool (Opcional)

### Para efectos visuales y proyectiles:
1. Crear GameObject vacío "ObjectPool"
2. Añadir componente `ObjectPool` (namespace Managers)
3. Configurar pools:
   ```
   Configuraciones:
     - Pool Id: "VFX_Hit"
       Prefab: (prefab de efecto de golpe)
       Tamaño Inicial: 10
       Tamaño Maximo: 30
       Expandir Si Necesario: true
     
     - Pool Id: "Proyectil_Fuego"
       Prefab: (prefab de proyectil)
       Tamaño Inicial: 5
       Tamaño Maximo: 20
   ```

### Uso en código:
```csharp
// Obtener objeto del pool
GameObject efecto = ObjectPool.Instance.Obtener("VFX_Hit", posicion, rotacion);

// Devolver al pool después de 2 segundos
ObjectPool.Instance.DevolverDespuesDe(efecto, 2f);
```

---

## 10. Configurar UI Reactiva (Opcional)

### Para barras de vida reactivas:
1. Crear Canvas con elementos de UI
2. En la barra de vida, añadir `BarraReactiva`:
   ```
   Barra Fill: (Image con fillAmount)
   Texto Valor: (TextMeshPro para "100/100")
   Id Registro: "BarraVida_Jugador1"
   Velocidad Animacion: 5
   Color Gradient: (gradiente rojo a verde)
   ```

### Para paneles de entidad:
1. Añadir `PanelEntidad` al panel:
   ```
   Texto Nombre: (TMP del nombre)
   Texto Nivel: (TMP del nivel)
   Barra Vida: (BarraReactiva)
   Barra Mana: (BarraReactiva)
   Indicador Turno: (GameObject que se activa en su turno)
   ```

---

## 11. Sistema de Guardado

### Configuración inicial:
El SaveSystem crea automáticamente la carpeta de guardados en:
```
Windows: %APPDATA%/../LocalLow/[CompanyName]/[ProductName]/Saves/
```

### Uso básico:
```csharp
// Crear datos de guardado
SaveData datos = SaveData.CrearNuevo();
datos.nivelJugador = jugador.Nivel_Entidad;
datos.vidaActual = jugador.VidaActual_Entidad;
// ... más datos

// Guardar
SaveSystem.Guardar("slot1", datos);

// Cargar
SaveData cargado = SaveSystem.Cargar("slot1");

// Auto-guardado
SaveSystem.AutoGuardar(datos);
```

---

## 12. Verificación Final

### Checklist:
- [ ] GameConfig creado en Resources
- [ ] Al menos un ClaseData para jugador
- [ ] Al menos un EnemigoData para enemigos
- [ ] HabilidadData de ataque básico
- [ ] EntityController en jugadores con ClaseData asignado
- [ ] EnemyController en enemigos con EnemigoData asignado
- [ ] CombateManager con referencias a controllers
- [ ] Escena guarda y funciona

### Probar el combate:
1. Play en Unity
2. Debería ver logs en consola:
   ```
   Entidad inicializada: Guerrero (Nivel 1)
   Enemigo inicializado: Goblin [Nv.1]
   === COMBATE INICIADO ===
   ```

---

## Troubleshooting

### Error: "NullReferenceException en EntityController"
- Verificar que ClaseData está asignado
- Verificar que EntityStats está en el mismo GameObject

### Error: "No tiene habilidad por defecto"
- Asignar HabilidadData en "Habilidad Por Defecto"

### Error: "GameConfig.Instance es null"
- Crear GameConfig en Assets/Resources
- Nombrarlo exactamente "GameConfig"

### Los elementos no se aplican:
- Verificar que GameConfig tiene los ElementDefinition
- Verificar que el ElementAttribute del ClaseData coincide

### El enemigo no ataca:
- Verificar que EnemigoData tiene HabilidadPorDefecto asignada
- Verificar que el enemigo tiene EnemyController (no EntityController)
