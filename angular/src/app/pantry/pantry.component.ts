import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { CoreModule, ListService, PagedResultDto } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPagination } from '@ng-bootstrap/ng-bootstrap';
import { Subscription } from 'rxjs';

import { ItemDespensaService } from '../proxy/despensa/item-despensa.service';
import { CreateItemDespensaDto, ItemDespensaDto } from '../proxy/despensa/models';
import { ProductoService } from '../proxy/productos/producto.service';
import { ProductoDto } from '../proxy/productos/models';

// Validador de fecha: el campo puede estar vacío, pero si tiene valor debe ser una fecha real y parseable.
function validDate(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) {
    return null;
  }

  const strValue = String(value);
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(strValue);
  if (match) {
    const year = Number(match[1]);
    const month = Number(match[2]);
    const day = Number(match[3]);
    const date = new Date(year, month - 1, day);
    const isReal =
      date.getFullYear() === year &&
      date.getMonth() === month - 1 &&
      date.getDate() === day;
    return isReal ? null : { invalidDate: true };
  }

  return isNaN(Date.parse(strValue)) ? { invalidDate: true } : null;
}

@Component({
  selector: 'app-pantry',
  standalone: true,
  imports: [CommonModule, CoreModule, ReactiveFormsModule, NgbPagination],
  providers: [ListService],
  templateUrl: './pantry.component.html',
  styleUrls: ['./pantry.component.scss']
})
export class PantryComponent implements OnInit, OnDestroy {
  // Estado del listado
  items: ItemDespensaDto[] = [];
  totalCount = 0;
  isLoading = true;
  hasError = false;

  // Catálogo de productos disponibles para el selector de alta
  productos: ProductoDto[] = [];

  // Estado del alta (RF-10)
  isCreateModalOpen = false;
  isSubmitting = false;
  createForm!: FormGroup;
  createErrorMessage = false;

  // Estado de la edición
  isModalOpen = false;
  isSaving = false;
  form!: FormGroup;
  selectedItemId?: string | null = null;

  // Mensajes de confirmación tras guardar (se ocultan solos a los 3 segundos)
  createSuccessMessage = false;
  updateSuccessMessage = false;
  private successTimeout?: ReturnType<typeof setTimeout>;
  private createSuccessTimeout?: ReturnType<typeof setTimeout>;

  private listSubscription?: Subscription;

  constructor(
    public readonly list: ListService,
    private itemDespensaService: ItemDespensaService,
    private productoService: ProductoService,
    private fb: FormBuilder,
    private confirmation: ConfirmationService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.buildForm();
    this.buildCreateForm();
    this.loadProductos();
    this.loadItems();
  }

  ngOnDestroy(): void {
    this.listSubscription?.unsubscribe();
    clearTimeout(this.successTimeout);
    clearTimeout(this.createSuccessTimeout);
  }

  // Formulario reactivo para alta de ítem (RF-10)
  buildCreateForm(): void {
    this.createForm = this.fb.group({
      productoId: [null, [Validators.required]],
      cantidad: [null, [Validators.min(0)]],
      unidad: [null, [Validators.maxLength(50)]],
      fechaVencimiento: [null, [validDate]]
    });
  }

  // Formulario reactivo para edición de ítem
  buildForm(): void {
    this.form = this.fb.group({
      cantidad: [null, [Validators.min(0)]],
      unidad: [null, [Validators.maxLength(50)]],
      fechaVencimiento: [null, [validDate]]
    });
  }

  loadProductos(): void {
    this.productoService
      .getList({ maxResultCount: 1000, skipCount: 0 }, { skipHandleError: true })
      .subscribe({
        next: (response: PagedResultDto<ProductoDto>) => {
          this.productos = response?.items || [];
          this.cdr.detectChanges();
        },
        error: () => {
          this.productos = [];
        }
      });
  }

  loadItems(): void {
    this.isLoading = true;
    this.hasError = false;

    this.listSubscription?.unsubscribe();

    const streamCreator = (query: any) =>
      this.itemDespensaService.getList(query, { skipHandleError: true });

    this.listSubscription = this.list.hookToQuery(streamCreator).subscribe({
      next: (response: PagedResultDto<ItemDespensaDto>) => {
        this.items = response?.items || [];
        this.totalCount = response?.totalCount ?? 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.hasError = true;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });

    this.list.get();
  }

  retry(): void {
    this.loadItems();
  }

  onPageChange(page: number): void {
    this.list.page = page - 1;
  }

  // --- Alta de ítem en despensa (RF-10) ---

  openCreateModal(): void {
    this.isModalOpen = false; // Cierra edición si estaba abierta
    this.createErrorMessage = false;
    this.createForm.reset({
      productoId: null,
      cantidad: null,
      unidad: null,
      fechaVencimiento: null
    });
    this.isCreateModalOpen = true;
  }

  cancelCreate(): void {
    this.isCreateModalOpen = false;
    this.createErrorMessage = false;
    this.createForm.reset();
  }

  saveNewItem(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.createErrorMessage = false;

    const raw = this.createForm.value;
    const payload: CreateItemDespensaDto = {
      productoId: raw.productoId,
      cantidad: raw.cantidad !== null && raw.cantidad !== '' ? Number(raw.cantidad) : null,
      unidad: raw.unidad?.trim() ? raw.unidad.trim() : null,
      fechaVencimiento: raw.fechaVencimiento ? raw.fechaVencimiento : null
    };

    this.itemDespensaService.create(payload, { skipHandleError: true }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.isCreateModalOpen = false;
        this.createForm.reset();
        this.showCreateSuccess();
        this.list.get();
        this.cdr.detectChanges();
      },
      error: () => {
        // Mantiene abierto el formulario con los datos ingresados y muestra error controlado
        this.isSubmitting = false;
        this.createErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  private showCreateSuccess(): void {
    clearTimeout(this.createSuccessTimeout);
    this.createSuccessMessage = true;
    this.createSuccessTimeout = setTimeout(() => {
      this.createSuccessMessage = false;
      this.cdr.detectChanges();
    }, 3000);
  }

  // --- Edición, consumo y eliminación ---

  edit(item: ItemDespensaDto): void {
    this.isCreateModalOpen = false; // Cierra alta si estaba abierta
    this.selectedItemId = item.id;

    this.form.reset({
      cantidad: item.cantidad ?? null,
      unidad: item.unidad ?? null,
      fechaVencimiento: this.toDateInputValue(item.fechaVencimiento)
    });

    this.isModalOpen = true;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.isSaving || !this.selectedItemId) {
      return;
    }

    this.isSaving = true;

    this.itemDespensaService.update(this.selectedItemId, this.form.value).subscribe({
      next: () => {
        this.isModalOpen = false;
        this.isSaving = false;
        this.showUpdateSuccess();
        this.list.get();
      },
      error: () => {
        this.isSaving = false;
      }
    });
  }

  cancelEdit(): void {
    this.isModalOpen = false;
  }

  consume(id?: string): void {
    if (!id) return;
    this.itemDespensaService.marcarComoConsumido(id).subscribe(() => {
      this.list.get();
    });
  }

  delete(id?: string): void {
    if (!id) return;

    this.confirmation
      .warn('::Pantry:DeleteConfirmationMessage', '::Pantry:DeleteConfirmationTitle')
      .subscribe((status) => {
        if (status === Confirmation.Status.confirm) {
          this.itemDespensaService.delete(id).subscribe(() => this.list.get());
        }
      });
  }

  private showUpdateSuccess(): void {
    clearTimeout(this.successTimeout);
    this.updateSuccessMessage = true;
    this.successTimeout = setTimeout(() => {
      this.updateSuccessMessage = false;
      this.cdr.detectChanges();
    }, 3000);
  }

  private toDateInputValue(value?: string | null): string | null {
    return value ? String(value).substring(0, 10) : null;
  }
}
