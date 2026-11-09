import { mapEnumToOptions } from '@abp/ng.core';

export enum EstadoBusquedaProducto {
  Encontrado = 0,
  NoEncontrado = 1,
  LimiteSolicitudes = 2,
  ServicioNoDisponible = 3,
}

export const estadoBusquedaProductoOptions = mapEnumToOptions(EstadoBusquedaProducto);
