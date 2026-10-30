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
import { ItemDespensaDto } from '../proxy/despensa/models';

// Validador de fecha: el campo puede estar vacío, pero si tiene valor debe ser una fecha parseable.
function validDate(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) {
    return null;
  }
  return isNaN(Date.parse(value)) ? { invalidDate: true } : null;
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

  // Estado de la edición
  isModalOpen = false;
  isSaving = false;
  form!: FormGroup; // Se inicializa en ngOnInit (buildForm)
  selectedItemId?: string | null = null; // Acepta undefined para coincidir con el tipo generado por ABP

  private listSubscription?: Subscription;

  constructor(
    public readonly list: ListService,
    private itemDespensaService: ItemDespensaService,
    private fb: FormBuilder,
    private confirmation: ConfirmationService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.buildForm();
    this.loadItems();
  }

  ngOnDestroy(): void {
    this.listSubscription?.unsubscribe();
  }

  // Formulario reactivo con las mismas reglas que valida el servidor (por usabilidad; el servidor sigue validando)
  buildForm(): void {
    this.form = this.fb.group({
      cantidad: [null, [Validators.min(0)]],
      unidad: [null, [Validators.maxLength(50)]],
      fechaVencimiento: [null, [validDate]]
    });
  }

  loadItems(): void {
    this.isLoading = true;
    this.hasError = false;

    // Si ya había una suscripción previa (por ejemplo, tras un error), se descarta antes de crear otra.
    this.listSubscription?.unsubscribe();

    // skipHandleError: el componente maneja el error del listado con su propio estado y botón de reintentar.
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

  // Tras un error el stream de hookToQuery termina, por eso se vuelve a crear la suscripción completa.
  retry(): void {
    this.loadItems();
  }

  onPageChange(page: number): void {
    // ngb-pagination es 1-based; ListService trabaja con índice de página base 0.
    this.list.page = page - 1;
  }

  // --- Edición, consumo y eliminación ---

  edit(item: ItemDespensaDto): void {
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
        this.list.get();
      },
      error: () => {
        // ABP muestra el mensaje del servidor; el panel queda abierto y el formulario conserva lo ingresado.
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

  // Toma los primeros 10 caracteres (yyyy-MM-dd) sin convertir a UTC, para no correr el día por la zona horaria.
  private toDateInputValue(value?: string | null): string | null {
    return value ? String(value).substring(0, 10) : null;
  }
}
