# Privacidad y datos de red

EZ Scene Collab 0.5.0 no incluye cuentas, publicidad, analítica, telemetría ni servicio en la nube. No envía intencionadamente datos del proyecto al editor del producto.

Durante una sesión, los editores de Unity intercambian nombres visibles, direcciones de red, estado de objetos y componentes, información de jerarquía, datos de meshes generadas e identificadores, rutas y hashes de assets. Los archivos de assets no se transfieren.

El host guarda en memoria un historial limitado para la reconexión, que se elimina al terminar la sesión. El idioma de la interfaz se guarda localmente en las preferencias del Editor de Unity.

El transporte actual es TCP sin cifrado ni autenticación integrada. Cualquier persona que pueda alcanzar el puerto abierto puede intentar conectarse. Utilízalo únicamente con colaboradores de confianza en una LAN segura o VPN privada, limita el firewall y no expongas proyectos confidenciales mediante un puerto público sin restricciones.

Tailscale y LogMeIn Hamachi son servicios externos opcionales con sus propias políticas de privacidad y condiciones.

[Volver al manual en español](README.md)
