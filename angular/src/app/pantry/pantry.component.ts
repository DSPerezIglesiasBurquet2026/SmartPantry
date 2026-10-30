import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CoreModule, ListService, PagedResultDto } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared'; // Servicio de confirmación requerido

import { ItemDespensaService } from '../proxy/despensa/item-despensa.service';
import { ItemDespensaDto } from '../proxy/despensa/models';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
@Component({
  selector: 'app-pantry',
  standalone: true,
  // 👇 Se agrega ReactiveFormsModule[cite: 3]
  imports: [CommonModule, CoreModule, ReactiveFormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './pantry.component.html',
  styleUrls: ['./pantry.component.scss']
})
export class PantryComponent implements OnInit {
  items: ItemDespensaDto[] = [];
  totalCount = 0;
  isLoading = true;
  hasError = false;
  updateSuccessMessage = false;
  // Variables para la edición[cite: 3]
  isModalOpen = false;
  isSaving = false;
  form!: FormGroup;// Agregamos '!' para decirle a TS que lo inicializaremos luego (en ngOnInit)
  selectedItemId?: string | null = null; // Aceptamos undefined para que coincida con el tipo generado por ABP

  constructor(
    public readonly list: ListService,
    private itemDespensaService: ItemDespensaService,
    private fb: FormBuilder,
    private confirmation: ConfirmationService, // Inyectamos el servicio de confirmación[cite: 3]
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.buildForm();
    this.loadItems();
  }

  // Creación del formulario reactivo con validaciones[cite: 3]
  buildForm(): void {
    this.form = this.fb.group({
      cantidad: [null, [Validators.min(0)]], // Validación: cantidad no negativa[cite: 3]
      unidad: [null, [Validators.maxLength(50)]],
      fechaVencimiento: [null]
    });
  }

  loadItems(): void {
    this.isLoading = true;
    this.hasError = false;

    const streamCreator = (query: any) =>
      this.itemDespensaService.getList(query, { skipHandleError: true }).pipe(
        // Atrapamos el error aquí mismo antes de que ListService lo procese
        catchError(err => {
          this.hasError = true;
          this.isLoading = false;
          this.cdr.detectChanges(); // Forzamos la actualización de la pantalla
          return throwError(() => err);
        })
      );

    this.list.hookToQuery(streamCreator).subscribe({
      next: (response: any) => {
        this.items = response?.items || [];
        this.totalCount = response?.totalCount || 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        // Mantenemos este bloque por seguridad, aunque catchError hará el trabajo principal
        this.hasError = true;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });

    this.list.get();
  }

  retry(): void {
    this.list.get();
  }

  // --- MÉTODOS DEL PUNTO 6 ---

  // 1. Edición: Precargar datos
  edit(item: ItemDespensaDto): void {
    this.selectedItemId = item.id;

    const formattedDate = item.fechaVencimiento
      ? new Date(item.fechaVencimiento).toISOString().split('T')[0]
      : null;

    this.form.patchValue({
      cantidad: item.cantidad,
      unidad: item.unidad,
      fechaVencimiento: formattedDate
    });

    this.isModalOpen = true;
  }

  // Guardar cambios
  save(): void {
    if (this.form.invalid || !this.selectedItemId) return;

    this.isSaving = true;

    this.itemDespensaService.update(this.selectedItemId, this.form.value).subscribe({
      next: () => {
        this.isModalOpen = false;
        this.isSaving = false;
        this.updateSuccessMessage = true;
        setTimeout(() => this.updateSuccessMessage = false, 3000);
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

  // 2. Marcar como consumido (Recibe string opcional y valida)
  consume(id?: string): void {
    if (!id) return;
    this.itemDespensaService.marcarComoConsumido(id).subscribe(() => {
      this.list.get();
    });
  }

  // 3. Eliminar con confirmación (Recibe string opcional y valida)
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
  // 👇 Método para manejar el cambio de página
  onPageChange(page: number): void {
    // ListService usa índice cero, ngb-pagination usa índice 1
    this.list.page = page - 1;
  }

}
