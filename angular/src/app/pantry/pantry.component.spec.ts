import { describe, it, expect, beforeEach, vi, type Mock } from 'vitest';
import { Pipe, PipeTransform } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, Subject, throwError } from 'rxjs';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPagination } from '@ng-bootstrap/ng-bootstrap';

import { PantryComponent } from './pantry.component';
import { ItemDespensaService } from '../proxy/despensa/item-despensa.service';
import { ItemDespensaDto } from '../proxy/despensa/models';

// Mock del pipe de localización: devuelve la clave tal cual, sin depender del servidor de textos.
@Pipe({ name: 'abpLocalization', standalone: true })
class MockAbpLocalizationPipe implements PipeTransform {
  transform(value: string): string { return value; }
}

const buildItem = (overrides: Partial<ItemDespensaDto> = {}): ItemDespensaDto => ({
  id: '1',
  productoNombre: 'Arroz',
  consumido: false,
  ...overrides
});

describe('PantryComponent', () => {
  let component: PantryComponent;
  let fixture: ComponentFixture<PantryComponent>;

  // Mocks (vi.fn) para no depender de la red
  let mockListService: Record<'hookToQuery' | 'get', Mock>;
  let mockItemDespensaService: Record<'getList' | 'update' | 'delete' | 'marcarComoConsumido', Mock>;
  let mockConfirmationService: Record<'warn', Mock>;

  // Stream controlado desde los tests: simula la respuesta (o el fallo) del listado
  let stream$: Subject<any>;

  beforeEach(async () => {
    mockListService = { hookToQuery: vi.fn(), get: vi.fn() };
    mockItemDespensaService = {
      getList: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      marcarComoConsumido: vi.fn()
    };
    mockConfirmationService = { warn: vi.fn() };

    stream$ = new Subject<any>();
    mockListService.hookToQuery.mockReturnValue(stream$);

    await TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: ItemDespensaService, useValue: mockItemDespensaService },
        { provide: ConfirmationService, useValue: mockConfirmationService }
      ]
    })
      // Se reemplazan los imports del componente (CoreModule trae el pipe real) y el ListService local.
      .overrideComponent(PantryComponent, {
        set: {
          imports: [CommonModule, ReactiveFormsModule, NgbPagination, MockAbpLocalizationPipe],
          providers: [{ provide: ListService, useValue: mockListService }]
        }
      })
      .compileComponents();

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // Ejecuta ngOnInit una única vez
  });

  // --- Los cuatro estados ---

  it('estado de carga: muestra el indicador y no muestra la lista vacía mientras la respuesta está pendiente', () => {
    const el: HTMLElement = fixture.nativeElement;

    expect(component).toBeTruthy();
    expect(component.isLoading).toBe(true);
    expect(el.querySelector('.fa-spinner')).toBeTruthy();
    expect(el.querySelector('.fa-box-open')).toBeNull();
    expect(el.querySelector('table')).toBeNull();
  });

  it('estado de lista vacía: muestra el mensaje localizado', () => {
    stream$.next({ items: [], totalCount: 0 });
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.fa-box-open')).toBeTruthy();
    expect(el.querySelector('.fa-spinner')).toBeNull();
    expect(el.querySelector('table')).toBeNull();
  });

  it('estado con resultados: muestra la tabla con los ítems', () => {
    stream$.next({
      items: [buildItem({ cantidad: 2, unidad: 'kg', fechaVencimiento: '2026-12-01T00:00:00' })],
      totalCount: 1
    });
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('table')).toBeTruthy();
    expect(el.querySelectorAll('tbody tr').length).toBe(1);
    expect(el.querySelector('.fa-box-open')).toBeNull();
  });

  it('estado de error: muestra el mensaje y el botón de reintentar', () => {
    stream$.error(new Error('Error de red simulado'));
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(component.hasError).toBe(true);
    expect(component.isLoading).toBe(false);
    expect(el.querySelector('.alert-danger')).toBeTruthy();
    expect(el.querySelector('.alert-danger button')).toBeTruthy();
    expect(el.querySelector('.fa-box-open')).toBeNull();
  });

  it('reintentar vuelve a cargar el listado', () => {
    stream$.error(new Error('Error de red simulado'));
    // El stream anterior terminó con error: el reintento debe crear uno nuevo.
    mockListService.hookToQuery.mockReturnValue(new Subject<any>());

    component.retry();

    expect(mockListService.hookToQuery).toHaveBeenCalledTimes(2);
    expect(component.hasError).toBe(false);
    expect(component.isLoading).toBe(true);
  });

  it('RF-09: comunica los datos faltantes con un texto localizado, sin inventar valores', () => {
    stream$.next({ items: [buildItem()], totalCount: 1 }); // sin fecha, cantidad ni unidad
    fixture.detectChanges();

    const faltantes = fixture.nativeElement.querySelectorAll('.fst-italic');
    expect(faltantes.length).toBe(3);
  });

  // --- Edición ---

  it('no debe guardar la edición si la cantidad es negativa', () => {
    component.edit(buildItem({ cantidad: 2, unidad: 'kg' }));
    component.form.patchValue({ cantidad: -5 });

    component.save();

    expect(component.form.get('cantidad')?.hasError('min')).toBe(true);
    expect(mockItemDespensaService.update).not.toHaveBeenCalled();
  });

  it('no debe guardar la edición si la fecha es inválida', () => {
    component.edit(buildItem());
    component.form.patchValue({ fechaVencimiento: 'no-es-una-fecha' });

    component.save();

    expect(component.form.get('fechaVencimiento')?.hasError('invalidDate')).toBe(true);
    expect(mockItemDespensaService.update).not.toHaveBeenCalled();
  });

  it('debe guardar con datos válidos, cerrar el panel y refrescar la lista', () => {
    mockItemDespensaService.update.mockReturnValue(of(undefined));
    component.edit(buildItem({ cantidad: 2, unidad: 'kg' }));
    component.form.patchValue({ cantidad: 3 });
    mockListService.get.mockClear();

    component.save();

    expect(mockItemDespensaService.update).toHaveBeenCalledWith('1', expect.objectContaining({ cantidad: 3 }));
    expect(component.isModalOpen).toBe(false);
    expect(component.isSaving).toBe(false);
    expect(component.updateSuccessMessage).toBe(true);
    expect(mockListService.get).toHaveBeenCalled();
  });

  it('si el servidor rechaza los datos, mantiene el panel abierto sin perder lo ingresado', () => {
    mockItemDespensaService.update.mockReturnValue(throwError(() => new Error('Rechazado por el servidor')));
    component.edit(buildItem({ cantidad: 2, unidad: 'kg' }));
    component.form.patchValue({ cantidad: 7 });

    component.save();

    expect(component.isModalOpen).toBe(true);
    expect(component.isSaving).toBe(false);
    expect(component.form.value.cantidad).toBe(7);
  });

  // --- Consumo ---

  it('debe marcar un ítem como consumido y refrescar la lista', () => {
    mockItemDespensaService.marcarComoConsumido.mockReturnValue(of(undefined));
    mockListService.get.mockClear();

    component.consume('1');

    expect(mockItemDespensaService.marcarComoConsumido).toHaveBeenCalledWith('1');
    expect(mockListService.get).toHaveBeenCalled();
  });

  // --- Eliminación ---

  it('debe invocar al servidor si se confirma la eliminación', () => {
    mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.confirm));
    mockItemDespensaService.delete.mockReturnValue(of(undefined));
    mockListService.get.mockClear();

    component.delete('1');

    expect(mockConfirmationService.warn).toHaveBeenCalled();
    expect(mockItemDespensaService.delete).toHaveBeenCalledWith('1');
    expect(mockListService.get).toHaveBeenCalled();
  });

  it('no debe invocar al servidor si se cancela la eliminación', () => {
    mockConfirmationService.warn.mockReturnValue(of(Confirmation.Status.reject));

    component.delete('1');

    expect(mockConfirmationService.warn).toHaveBeenCalled();
    expect(mockItemDespensaService.delete).not.toHaveBeenCalled();
  });
});
