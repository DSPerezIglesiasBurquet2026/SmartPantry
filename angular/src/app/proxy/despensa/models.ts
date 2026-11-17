import type { AuditedEntityDto } from '@abp/ng.core';

export interface ItemDespensaDto extends AuditedEntityDto<string> {
  productoId?: string;
  productoNombre?: string;
  cantidad?: number | null;
  unidad?: string;
  fechaVencimiento?: string | null;
  consumido?: boolean;
}

export interface CreateItemDespensaDto {
  productoId: string;
  cantidad?: number | null;
  unidad?: string | null;
  fechaVencimiento?: string | null;
}

export interface UpdateItemDespensaDto {
  cantidad?: number | null;
  unidad?: string;
  fechaVencimiento?: string | null;
}
