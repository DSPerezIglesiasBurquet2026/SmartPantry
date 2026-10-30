import type { EntityDto } from '@abp/ng.core';
import type { EstadoBusquedaProducto } from './estado-busqueda-producto.enum';

export interface BuscarProductoPorCodigoInputDto {
  barcode: string;
}

export interface CreateProductoDto {
  codigoBarras: string;
  nombre: string;
  marca: string;
}

export interface CreateUpdateProductoDto {
  codigoBarras?: string;
  nombre: string;
  marca: string;
}

export interface ProductoDto extends EntityDto<string> {
  codigoBarras?: string;
  nombre?: string;
  marca?: string;
}

export interface ResultadoBusquedaProductoDto {
  barcode?: string;
  nombre?: string | null;
  marca?: string | null;
  imagenUrl?: string | null;
  categorias?: string[];
  ingredientes?: string[];
  alergenos?: string[];
  nutriScore?: string | null;
  novaGroup?: string | null;
  estado?: EstadoBusquedaProducto;
  encontrado?: boolean;
  mensajeError?: string | null;
}
