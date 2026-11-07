# PRD — Sistema de Gestión de DD&J

## Descripción

Tienda de gestión de una tienda de ropa y de personalización con distintas técnicas (sublimación, DTF etc.)
Esta dirigida a los dueños y empleados de la tienda.

## Objetivos

#### Fase inicial: Preparar entorno

- Estructura de carpetas y frameworks
- Pantallas y navegación básica entre módulos
- Definir AGENTS.md y carpetas de doc de IA
- Crear y preparar la BBDD (EF Core con enfoque code first)
- Crear repo en Git y conectarlo a gitHub

#### Fase 2: Contexto

- Llenar el backlog con las HU

## Módulos

Se definen los módulos o features que van a definir las pantallas y la navegación, esto va a crecer.

#### A implementar

1. **Módulo:** Login
2. **Módulo:** Inventario
   1. Dashboard resumen
   2. Registrar producto
   3. Movimiento de Entrada/Salida (compra/venta)
   4. Consumibles, insumos
3. **Módulo:** Servicio de personalización
   1. Calendario para Ver pedidos actuales
   2. Gestionar pedidos (Crearlos, Editarlos, eliminarlos)
   3. Planillas orden de servicio
   4. Calculo de presupuesto del servicio al cliente, calculadora lo que pida el cliente.
   5. Notificaciones a los usuarios y los clientes
4. **Modulo:** CRM
   1. Datos del clientes
   2. Pago de deudas del cliente
   3. Beneficios de clientes fieles

## Stack Técnico

#### Tecnologías y frameworks

- Frontend: Blazor
- Backend: ASP.NET
- BBDD: PostgreSQL

#### Arquitectura

- Monolito modular a nivel macro, Vertical Slices Architecture (VSA) a nivel micro
