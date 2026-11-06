import type { BuscarProductoPorCodigoInputDto, CreateProductoDto, CreateUpdateProductoDto, ProductoDto, ResultadoBusquedaProductoDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ProductoService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateProductoDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProductoDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/producto',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/producto/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProductoDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/producto/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getBuscarPorCodigo = (input: BuscarProductoPorCodigoInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ResultadoBusquedaProductoDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/producto/buscar-por-codigo',
      params: { barcode: input.barcode },
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ProductoDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/producto',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateProductoDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProductoDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/producto/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}