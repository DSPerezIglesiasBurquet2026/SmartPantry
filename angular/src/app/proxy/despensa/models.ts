import type { AuditedEntityDto } from '@abp/ng.core';

export interface ItemDespensaDto extends AuditedEntityDto<string> {
  productoId?: string;
  productoNombre?: string;
  cantidad?: number | null;
  unidad?: string;
  fechaVencimiento?: string | null;
  consumido?: boolean;
}

export interface UpdateItemDespensaDto {
  cantidad?: number | null;
  unidad?: string;
  fechaVencimiento?: string | null;
}
