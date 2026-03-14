import { Component, Prop, State, h } from '@stencil/core';

@Component({
  tag: 'vacation-request',
  styleUrl: 'vacation-request.css',
  shadow: true,
})
export class VacationRequest {
  @Prop() endpoint: string = '/widget/request';
  @Prop() apiKey: string = '';

  @State() private arrivalDate: string = '';
  @State() private stayDuration: number = 3;
  @State() private adultCount: number = 2;
  @State() private childrenCount: number = 0;
  @State() private childAges: number[] = [];
  @State() private lastName: string = '';
  @State() private firstName: string = '';
  @State() private email: string = '';
  @State() private phone: string = '';
  @State() private remarks: string = '';
  @State() private marketingConsent: boolean = false;
  @State() private errors: Record<string, string> = {};
  @State() private statusType: '' | 'error' | 'success' = '';
  @State() private statusMessage: string = '';
  @State() private isSubmitting: boolean = false;

  // ────────────────────────────── Date helpers ──────────────────────────────

  private parseIsoDate(iso: string): Date | null {
    if (!iso) return null;
    const parts = iso.split('-').map(p => parseInt(p, 10));
    if (parts.length !== 3 || parts.some(isNaN)) return null;
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }

  private formatDateLabel(d: Date): string {
    return d.toLocaleDateString('de-DE', {
      weekday: 'long',
      day: '2-digit',
      month: '2-digit',
    });
  }

  private toIso(d: Date): string {
    return [
      d.getFullYear(),
      String(d.getMonth() + 1).padStart(2, '0'),
      String(d.getDate()).padStart(2, '0'),
    ].join('-');
  }

  private get timeframePreview(): string {
    const start = this.parseIsoDate(this.arrivalDate);
    if (!start || this.stayDuration < 1) return 'Bitte Anreise und Dauer wählen.';
    const dep = new Date(start);
    dep.setDate(dep.getDate() + this.stayDuration);
    return `${this.formatDateLabel(start)} bis ${this.formatDateLabel(dep)}`;
  }

  private get departureDate(): string {
    const start = this.parseIsoDate(this.arrivalDate);
    if (!start) return '';
    const dep = new Date(start);
    dep.setDate(dep.getDate() + this.stayDuration);
    return this.toIso(dep);
  }

  // ────────────────────────────── Persons helpers ───────────────────────────

  private setChildrenCount(count: number): void {
    const prev = this.childAges;
    this.childrenCount = count;
    this.childAges = Array.from({ length: count }, (_, i) =>
      prev[i] !== undefined && !isNaN(prev[i]) ? prev[i] : NaN,
    );
  }

  private setChildAge(index: number, raw: string): void {
    const ages = [...this.childAges];
    ages[index] = parseInt(raw, 10);
    this.childAges = ages;
  }

  // ────────────────────────────── Validation ────────────────────────────────

  private validate(): boolean {
    const e: Record<string, string> = {};

    if (!this.arrivalDate) {
      e['arrivalDate'] = 'Bitte Anreisedatum angeben.';
    }
    if (this.stayDuration < 1) {
      e['stayDuration'] = 'Bitte gültige Aufenthaltsdauer wählen.';
    }
    if (this.adultCount < 1 || this.adultCount > 10) {
      e['adultCount'] = 'Bitte 1 bis 10 Erwachsene angeben.';
    }
    if (this.childrenCount < 0 || this.childrenCount > 6) {
      e['childrenCount'] = 'Bitte 0 bis 6 Kinder wählen.';
    }
    if (!this.lastName.trim()) {
      e['lastName'] = 'Nachname ist erforderlich.';
    }

    const em = this.email.trim();
    if (!em) {
      e['email'] = 'E-Mail ist erforderlich.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(em)) {
      e['email'] = 'Bitte eine gültige E-Mail-Adresse eingeben.';
    }

    if (this.childAges.some(age => isNaN(age) || age < 0 || age > 18)) {
      e['childrenAges'] = 'Bitte für jedes Kind ein Alter zwischen 0 und 18 eingeben.';
    }

    this.errors = e;
    return Object.keys(e).length === 0;
  }

  // ────────────────────────────── Submit ────────────────────────────────────

  private async handleSubmit(ev: Event): Promise<void> {
    ev.preventDefault();
    this.statusType = '';
    this.statusMessage = '';

    if (!this.validate()) {
      this.statusType = 'error';
      this.statusMessage = 'Bitte korrigieren Sie die markierten Felder.';
      return;
    }

    this.isSubmitting = true;

    const personAges: number[] = [
      ...Array.from({ length: this.adultCount }, () => 30),
      ...this.childAges,
    ];

    const payload = {
      FirstName: this.firstName.trim(),
      LastName: this.lastName.trim(),
      Email: this.email.trim(),
      Phone: this.phone.trim(),
      ArrivalDate: this.arrivalDate,
      DepartureDate: this.departureDate,
      PersonAges: personAges,
      Remarks: this.remarks.trim(),
      MarketingConsent: this.marketingConsent,
    };

    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (this.apiKey) headers['X-Api-Key'] = this.apiKey;

    try {
      const res = await fetch(this.endpoint, {
        method: 'POST',
        headers,
        body: JSON.stringify(payload),
      });

      let body: { message?: string } | null = null;
      try {
        body = await res.json();
      } catch {
        // empty – response may have no body
      }

      if (!res.ok) {
        throw new Error(body?.message ?? 'Senden fehlgeschlagen. Bitte später erneut versuchen.');
      }

      this.statusType = 'success';
      this.statusMessage = body?.message ?? 'Ihre Anfrage wurde erfolgreich übermittelt.';
      this.resetForm();
    } catch (err) {
      this.statusType = 'error';
      this.statusMessage = (err as Error).message || 'Unbekannter Fehler beim Senden.';
    } finally {
      this.isSubmitting = false;
    }
  }

  private resetForm(): void {
    this.arrivalDate = '';
    this.stayDuration = 3;
    this.adultCount = 2;
    this.childrenCount = 0;
    this.childAges = [];
    this.lastName = '';
    this.firstName = '';
    this.email = '';
    this.phone = '';
    this.remarks = '';
    this.marketingConsent = false;
    this.errors = {};
  }

  // ────────────────────────────── Render helpers ────────────────────────────

  private err(field: string): string {
    return this.errors[field] ?? '';
  }

  private invalid(field: string): 'true' | 'false' {
    return this.errors[field] ? 'true' : 'false';
  }

  // ────────────────────────────── Render ────────────────────────────────────

  render() {
    const durationOpts: [number, string][] = [
      [1, '1 Nacht'],
      [2, '2 Nächte'],
      [3, '3 Nächte'],
      [4, '4 Nächte'],
      [5, '5 Nächte'],
      [6, '6 Nächte'],
      [7, '7 Nächte'],
      [8, '8 Nächte'],
      [9, '9 Nächte'],
      [10, '10 Nächte'],
      [14, '14 Nächte'],
    ];

    return (
      <div class="page">
        {/* ── Hero ── */}
        <section class="hero">
          <span class="kicker">Hotel Anfrage</span>
          <h1>Urlaubsanfrage senden</h1>
          <p>
            Bitte Zeitraum und Personenzusammenstellung eintragen. Kinder werden
            mit Alter bis 18 Jahre abgefragt.
          </p>
        </section>

        {/* ── Form panel ── */}
        <section class="panel">
          <form noValidate onSubmit={(e: Event) => this.handleSubmit(e)}>

            {/* ─── Zeitraum ─── */}
            <fieldset>
              <legend>Zeitraum</legend>
              <div class="timeframe-row">
                <div class="field">
                  <label htmlFor="arrivalDate" class="required">Anreise</label>
                  <input
                    id="arrivalDate"
                    type="date"
                    required
                    autocomplete="off"
                    value={this.arrivalDate}
                    aria-invalid={this.invalid('arrivalDate')}
                    onInput={(e: Event) => {
                      this.arrivalDate = (e.target as HTMLInputElement).value;
                    }}
                  />
                  {this.err('arrivalDate') && (
                    <div class="field-error">{this.err('arrivalDate')}</div>
                  )}
                </div>

                <div class="field">
                  <label htmlFor="stayDuration" class="required">Dauer</label>
                  <select
                    id="stayDuration"
                    required
                    aria-invalid={this.invalid('stayDuration')}
                    onChange={(e: Event) => {
                      this.stayDuration = parseInt(
                        (e.target as HTMLSelectElement).value, 10,
                      );
                    }}
                  >
                    {durationOpts.map(([val, label]) => (
                      <option key={val} value={val} selected={this.stayDuration === val}>
                        {label}
                      </option>
                    ))}
                  </select>
                  {this.err('stayDuration') && (
                    <div class="field-error">{this.err('stayDuration')}</div>
                  )}
                </div>
              </div>

              <p class="timeframe-preview">{this.timeframePreview}</p>
            </fieldset>

            {/* ─── Personen ─── */}
            <fieldset>
              <legend>Personenzusammenstellung</legend>

              <div class="guest-grid">
                <div class="field">
                  <label htmlFor="adultCount" class="required">Erwachsene</label>
                  <select
                    id="adultCount"
                    required
                    aria-invalid={this.invalid('adultCount')}
                    onChange={(e: Event) => {
                      this.adultCount = parseInt(
                        (e.target as HTMLSelectElement).value, 10,
                      );
                    }}
                  >
                    {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map(n => (
                      <option key={n} value={n} selected={this.adultCount === n}>
                        {n}
                      </option>
                    ))}
                  </select>
                  {this.err('adultCount') && (
                    <div class="field-error">{this.err('adultCount')}</div>
                  )}
                </div>

                <div class="field">
                  <label htmlFor="childrenCount" class="required">Kinder</label>
                  <select
                    id="childrenCount"
                    required
                    aria-invalid={this.invalid('childrenCount')}
                    onChange={(e: Event) => {
                      this.setChildrenCount(
                        parseInt((e.target as HTMLSelectElement).value, 10),
                      );
                    }}
                  >
                    {[0, 1, 2, 3, 4, 5, 6].map(n => (
                      <option key={n} value={n} selected={this.childrenCount === n}>
                        {n}
                      </option>
                    ))}
                  </select>
                  {this.err('childrenCount') && (
                    <div class="field-error">{this.err('childrenCount')}</div>
                  )}
                </div>
              </div>

              {this.childAges.length > 0 && (
                <div class="field">
                  <label>Kinderalter (0 bis 18 Jahre)</label>
                  <div class="children-list" aria-live="polite">
                    {this.childAges.map((age, i) => (
                      <div class="child-row" key={i}>
                        <div class="field">
                          <label htmlFor={`childAge${i + 1}`} class="required">
                            Kind {i + 1} Alter (0–18)
                          </label>
                          <input
                            id={`childAge${i + 1}`}
                            type="number"
                            min={0}
                            max={18}
                            step={1}
                            inputMode="numeric"
                            placeholder="z. B. 7"
                            required
                            value={isNaN(age) ? '' : String(age)}
                            aria-invalid={
                              isNaN(age) || age < 0 || age > 18 ? 'true' : 'false'
                            }
                            onInput={(e: Event) =>
                              this.setChildAge(
                                i,
                                (e.target as HTMLInputElement).value,
                              )
                            }
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                  <span class="hint">
                    Anzahl der Felder folgt der Kinder-Auswahl.
                  </span>
                  {this.err('childrenAges') && (
                    <div class="field-error">{this.err('childrenAges')}</div>
                  )}
                </div>
              )}
            </fieldset>

            {/* ─── Kontaktdaten ─── */}
            <fieldset>
              <legend>Kontaktdaten</legend>

              <div class="form-row-horizontal">
                <div class="field">
                  <label htmlFor="lastName" class="required">Nachname</label>
                  <input
                    id="lastName"
                    type="text"
                    required
                    maxLength={80}
                    autocomplete="family-name"
                    value={this.lastName}
                    aria-invalid={this.invalid('lastName')}
                    onInput={(e: Event) => {
                      this.lastName = (e.target as HTMLInputElement).value;
                    }}
                  />
                  {this.err('lastName') && (
                    <div class="field-error">{this.err('lastName')}</div>
                  )}
                </div>

                <div class="field">
                  <label htmlFor="firstName">Vorname (optional)</label>
                  <input
                    id="firstName"
                    type="text"
                    maxLength={80}
                    autocomplete="given-name"
                    value={this.firstName}
                    onInput={(e: Event) => {
                      this.firstName = (e.target as HTMLInputElement).value;
                    }}
                  />
                </div>
              </div>

              <div class="form-row">
                <div class="field">
                  <label htmlFor="email" class="required">E-Mail</label>
                  <input
                    id="email"
                    type="email"
                    required
                    maxLength={120}
                    autocomplete="email"
                    inputMode="email"
                    value={this.email}
                    aria-invalid={this.invalid('email')}
                    onInput={(e: Event) => {
                      this.email = (e.target as HTMLInputElement).value;
                    }}
                  />
                  {this.err('email') && (
                    <div class="field-error">{this.err('email')}</div>
                  )}
                </div>

                <div class="field">
                  <label htmlFor="phone">Telefon (optional)</label>
                  <input
                    id="phone"
                    type="tel"
                    maxLength={40}
                    autocomplete="tel"
                    inputMode="tel"
                    value={this.phone}
                    onInput={(e: Event) => {
                      this.phone = (e.target as HTMLInputElement).value;
                    }}
                  />
                </div>
              </div>

              <div class="field">
                <label htmlFor="remarks">Bemerkung (optional)</label>
                <textarea
                  id="remarks"
                  rows={4}
                  maxLength={1000}
                  placeholder="Wunschzimmer, Unverträglichkeiten, Anmerkungen..."
                  onInput={(e: Event) => {
                    this.remarks = (e.target as HTMLTextAreaElement).value;
                  }}
                >
                  {this.remarks}
                </textarea>
              </div>

              <label class="consent" htmlFor="marketingConsent">
                <input
                  type="checkbox"
                  id="marketingConsent"
                  checked={this.marketingConsent}
                  onChange={(e: Event) => {
                    this.marketingConsent = (e.target as HTMLInputElement).checked;
                  }}
                />
                <span>Ich möchte gelegentlich Informationen zu Angeboten erhalten.</span>
              </label>
            </fieldset>

            {/* ─── Submit ─── */}
            <section class="submit-wrap">
              {this.statusMessage && (
                <div
                  class={`status show ${this.statusType}`}
                  role="status"
                  aria-live="polite"
                >
                  {this.statusMessage}
                </div>
              )}
              <button type="submit" class="submit-btn" disabled={this.isSubmitting}>
                {this.isSubmitting ? 'Wird gesendet…' : 'Urlaubsanfrage absenden'}
              </button>
            </section>

          </form>
        </section>
      </div>
    );
  }
}
