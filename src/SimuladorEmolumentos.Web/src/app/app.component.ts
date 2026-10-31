import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';
import { CalculationResult, CatalogEntry, SimulationService } from './simulation.service';

@Component({ selector: 'app-root', standalone: true, imports: [ReactiveFormsModule, CurrencyPipe], templateUrl: './app.component.html' })
export class AppComponent {
  private readonly api = inject(SimulationService);
  private readonly destroyRef = inject(DestroyRef);
  private calculation?: Subscription;
  readonly catalog = signal<CatalogEntry[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly copied = signal('');
  readonly result = signal<CalculationResult | null>(null);
  readonly selected = signal<CatalogEntry | undefined>(undefined);
  readonly states = computed(() => [...new Set(this.catalog().map(r => r.state))].sort());
  readonly acts = computed(() => this.catalog().filter(r => r.state === this.selected()?.state));
  readonly form = new FormGroup({
    state: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    actId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    base: new FormControl('', { nonNullable: true })
  });
  constructor() {
    this.form.controls.state.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.selectState());
    this.form.controls.actId.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.selectAct());
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.invalidate());
    this.loadCatalog();
  }
  loadCatalog() {
    this.loading.set(true); this.error.set('');
    this.api.catalog().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: rows => {
        this.catalog.set(rows); this.loading.set(false);
        if (!rows.length) { this.error.set('Nenhuma regra disponível no catálogo.'); return; }
        this.reset();
      },
      error: () => { this.loading.set(false); this.error.set('Não foi possível carregar o catálogo. Verifique a API e tente novamente.'); }
    });
  }
  private selectState() {
    const first = this.catalog().find(r => r.state === this.form.controls.state.value);
    this.form.controls.actId.setValue(first?.actId ?? '', { emitEvent: false });
    this.selectAct();
  }
  private selectAct() {
    const rule = this.catalog().find(r => r.state === this.form.controls.state.value && r.actId === this.form.controls.actId.value);
    this.selected.set(rule);
    const base = this.form.controls.base;
    base.reset('', { emitEvent: false });
    if (rule?.requiresBase) {
      base.setValidators([Validators.required, control => {
        const text = String(control.value).trim();
        const value = Number(text.replace(',', '.'));
        return /^\d+(,\d{1,2})?$/.test(text) && value > 0 && value <= 1_000_000_000 ? null : { amount: true };
      }]);
      base.enable({ emitEvent: false });
    } else { base.clearValidators(); base.disable({ emitEvent: false }); }
    base.updateValueAndValidity({ emitEvent: false });
    this.invalidate();
  }
  private invalidate() {
    this.calculation?.unsubscribe(); this.busy.set(false);
    this.result.set(null); this.error.set(''); this.copied.set('');
  }
  reset() {
    this.form.reset({ state: this.states()[0] ?? '', actId: '', base: '' }, { emitEvent: false });
    this.selectState();
  }
  calculate() {
    if (this.busy() || this.loading()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid || !this.selected()) return;
    this.error.set(''); this.result.set(null); this.copied.set(''); this.busy.set(true);
    this.calculation = this.api.calculate({ state: this.form.controls.state.value, actId: this.form.controls.actId.value,
      baseAmount: this.selected()?.requiresBase ? Number(this.form.controls.base.value.trim().replace(',', '.')) : null
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => { this.result.set(result); this.busy.set(false); },
      error: (error: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(error.status === 400 && typeof error.error?.detail === 'string' ? error.error.detail : 'Não foi possível calcular. Verifique a conexão e tente novamente.');
      }
    });
  }
  async copy() {
    const result = this.result();
    if (!result) return;
    const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    const text = ['SIMULAÇÃO FICTÍCIA — SEM VALIDADE OFICIAL', `UF: ${this.selected()?.state}`, `Ato: ${this.selected()?.actName}`,
      `Regra: ${result.ruleId}`, result.formula, result.parameters, ...result.items.map(item => `${item.description}: ${money(item.amount)}`), `Total: ${money(result.total)}`].join('\n');
    try { await navigator.clipboard.writeText(text); this.copied.set('Memória copiada.'); }
    catch { this.copied.set('Não foi possível copiar. Selecione o resultado e use Ctrl+C.'); }
  }
}
