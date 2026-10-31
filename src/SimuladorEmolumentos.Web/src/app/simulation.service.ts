import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
export interface CatalogEntry { state: string; actId: string; actName: string; requiresBase: boolean; }
export interface CalculationRequest { state: string; actId: string; baseAmount: number | null; }
export interface CalculationResult { ruleId: string; formula: string; parameters: string; items: { description: string; amount: number }[]; total: number; }
@Injectable({ providedIn: 'root' })
export class SimulationService {
  private readonly http = inject(HttpClient);
  catalog() { return this.http.get<CatalogEntry[]>('/api/catalog'); }
  calculate(request: CalculationRequest) { return this.http.post<CalculationResult>('/api/simulations', request); }
}
