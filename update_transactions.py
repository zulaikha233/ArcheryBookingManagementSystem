import re

file_path = 'Views/Payment/Fee/MemberPayment.cshtml'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace header section
header_regex = re.compile(r'<div class="transactions-card" style="margin-top: 0;">\s*<div class="trans-header-wrap">.*?<div class="table-responsive">\s*<table[^>]+>.*?</thead>\s*<tbody>', re.DOTALL)

new_header = '''<!-- Transaction List -->
            <div class="page-header d-flex justify-content-between align-items-end mb-4">
                <div>
                    <h1 class="text-white">Transaction</h1>
                    <p class="text-dim">Reserve courts, buy equipment, and pay coaching fees with just a few taps.</p>
                </div>
                
                <div class="filter-controls d-flex gap-2">
                    <div class="search-wrapper" style="position: relative;">
                        <input type="text" id="transactionSearch" class="search-input-premium" placeholder="Search" style="padding-left: 35px; background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; color: #fff; height: 38px;">
                        <i class="bi bi-search search-icon-inside" style="position: absolute; left: 10px; top: 10px; color: var(--text-dim);"></i>
                    </div>
                    
                    <div class="select-premium-wrapper" style="position: relative;">
                        <select class="select-premium" id="dateFilter" style="background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; color: #fff; height: 38px; padding: 0 10px;">
                            <option value="All">This Week</option>
                            <option value="All">All Time</option>
                        </select>
                    </div>

                    <div class="select-premium-wrapper" style="position: relative;">
                        <select class="select-premium" id="statusFilter" style="background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; color: #fff; height: 38px; padding: 0 10px;">
                            <option value="All">All Transactions</option>
                            <option value="Paid">Paid</option>
                            <option value="Pending">Pending</option>
                        </select>
                    </div>
                </div>
            </div>

            <div style="display: flex; flex-direction: column; gap: 14px;">
                <!-- Column Headers -->
                <div style="padding: 0 18px 5px 18px; border-bottom: 1px solid rgba(255,255,255,0.05); margin-bottom: 10px; display: flex; align-items: center; justify-content: space-between; gap: 14px;">
                    <div style="flex: 1; min-width: 120px; font-size: 0.68rem; font-weight: 800; text-transform: uppercase; letter-spacing: 1.5px; color: var(--text-dim); text-align: center;">DATE</div>
                    <div style="flex: 1.5; min-width: 180px; font-size: 0.68rem; font-weight: 800; text-transform: uppercase; letter-spacing: 1.5px; color: var(--text-dim); text-align: center;">TRANSACTION TYPE</div>
                    <div style="flex: 1.5; min-width: 150px; font-size: 0.68rem; font-weight: 800; text-transform: uppercase; letter-spacing: 1.5px; color: var(--text-dim); text-align: center;">FOR ARCHER</div>
                    <div style="flex: 1; min-width: 100px; font-size: 0.68rem; font-weight: 800; text-transform: uppercase; letter-spacing: 1.5px; color: var(--text-dim); text-align: center;">PAYMENT</div>
                    <div style="flex: 1; min-width: 100px; font-size: 0.68rem; font-weight: 800; text-transform: uppercase; letter-spacing: 1.5px; color: var(--text-dim); text-align: center;">STATUS</div>
                    <div style="width: 160px; font-size: 0.68rem; font-weight: 800; text-transform: uppercase; letter-spacing: 1.5px; color: var(--text-dim); text-align: center;">ACTION</div>
                </div>'''

content = header_regex.sub(new_header, content)

# Remove the closing table tags
content = content.replace('</tbody>\n                    </table>\n                </div>\n            </div>', '</div>')

# Replace row structures for Membership
mem_row_regex = re.compile(r'<tr class="trans-row" data-status="@statusText">\s*<td class="text-dim text-center">\s*<div class="fw-bold text-white">(@mp\.PaymentDate\.ToString\("MMM dd, yyyy"\))</div>\s*</td>\s*<td>\s*(.*?)\s*</td>\s*<td>\s*<div class="d-flex align-items-center gap-3">\s*<div class="avatar-cell-initial">(@Model\.Username\[0\].*?)</div>\s*<div class="fw-bold text-white">(@Model\.Username)</div>\s*</div>\s*</td>\s*<td class="text-end fw-bold text-white">\s*(RM @mp\.Amount.*?)\s*</td>\s*<td class="text-center">\s*<span class="@\(isPaid \? "badge-paid-pill" : "badge-pending-pill"\)">\s*(.*?)\s*</span>\s*</td>\s*<td class="text-end">\s*(.*?)\s*</td>\s*</tr>', re.DOTALL)

mem_row_repl = r'''<div class="trans-row" data-status="@statusText" style="background: rgba(255,255,255,0.02); border: 1px solid var(--border-color); border-radius: 14px; padding: 16px 18px; display: flex; align-items: center; justify-content: space-between; gap: 14px; transition: 0.3s; margin-bottom: 14px;">
                                    <div style="flex: 1; min-width: 120px; font-weight: 700; color: #fff; text-align: center; font-size: 0.95rem;">\1</div>
                                    <div style="flex: 1.5; min-width: 180px; text-align: center;">
                                        \2
                                    </div>
                                    <div style="flex: 1.5; min-width: 150px; display: flex; align-items: center; justify-content: center; gap: 12px;">
                                        <div class="avatar-cell-initial" style="width: 32px; height: 32px; font-size: 0.8rem;">\3</div>
                                        <div style="font-weight: 700; color: #fff; font-size: 0.95rem;">\4</div>
                                    </div>
                                    <div style="flex: 1; min-width: 100px; font-weight: 700; color: #fff; text-align: center; font-size: 0.95rem;">
                                        \5
                                    </div>
                                    <div style="flex: 1; min-width: 100px; display: flex; justify-content: center;">
                                        <span class="@(isPaid ? "badge-paid-pill" : "badge-pending-pill")">
                                            \6
                                        </span>
                                    </div>
                                    <div style="width: 160px; display: flex; justify-content: center; align-items: center;">
                                        \7
                                    </div>
                                </div>'''

content = mem_row_regex.sub(mem_row_repl, content)

# Replace row structures for Class
class_row_regex = re.compile(r'<tr class="trans-row" data-status="@statusText">\s*<!-- Date -->\s*<td class="text-dim text-center">\s*<div class="fw-bold text-white">(@c\.RegistrationDate\.ToString\("MMM dd, yyyy"\))</div>\s*</td>\s*<!-- Transaction Type -->\s*<td>\s*(.*?)\s*</td>\s*<!-- For Archer -->\s*<td>\s*<div class="d-flex align-items-center gap-3">\s*<div class="avatar-cell-initial">(@archerName\[0\].*?)</div>\s*<div class="fw-bold text-white">(@archerName)</div>\s*</div>\s*</td>\s*<!-- Payment -->\s*<td class="text-end fw-bold text-white">\s*(RM @c\.TotalPrice.*?)\s*</td>\s*<!-- Status -->\s*<td class="text-center">\s*<span class="@\(isPaid \? "badge-paid-pill" : "badge-pending-pill"\)">\s*(.*?)\s*</span>\s*</td>\s*<!-- Actions -->\s*<td class="text-end">\s*(.*?)\s*</td>\s*</tr>', re.DOTALL)

class_row_repl = r'''<div class="trans-row" data-status="@statusText" style="background: rgba(255,255,255,0.02); border: 1px solid var(--border-color); border-radius: 14px; padding: 16px 18px; display: flex; align-items: center; justify-content: space-between; gap: 14px; transition: 0.3s; margin-bottom: 14px;">
                                    <!-- Date -->
                                    <div style="flex: 1; min-width: 120px; font-weight: 700; color: #fff; text-align: center; font-size: 0.95rem;">\1</div>
                                    <!-- Transaction Type -->
                                    <div style="flex: 1.5; min-width: 180px; text-align: center;">
                                        \2
                                    </div>
                                    <!-- For Archer -->
                                    <div style="flex: 1.5; min-width: 150px; display: flex; align-items: center; justify-content: center; gap: 12px;">
                                        <div class="avatar-cell-initial" style="width: 32px; height: 32px; font-size: 0.8rem;">\3</div>
                                        <div style="font-weight: 700; color: #fff; font-size: 0.95rem;">\4</div>
                                    </div>
                                    <!-- Payment -->
                                    <div style="flex: 1; min-width: 100px; font-weight: 700; color: #fff; text-align: center; font-size: 0.95rem;">
                                        \5
                                    </div>
                                    <!-- Status -->
                                    <div style="flex: 1; min-width: 100px; display: flex; justify-content: center;">
                                        <span class="@(isPaid ? "badge-paid-pill" : "badge-pending-pill")">
                                            \6
                                        </span>
                                    </div>
                                    <!-- Actions -->
                                    <div style="width: 160px; display: flex; justify-content: center; align-items: center;">
                                        \7
                                    </div>
                                </div>'''

content = class_row_regex.sub(class_row_repl, content)

# Replace row structures for Reservations
res_row_regex = re.compile(r'<tr class="trans-row" data-status="@statusText">\s*<!-- Date -->\s*<td class="text-dim text-center">\s*<div class="fw-bold text-white">(@txnDate\.ToString\("MMM dd, yyyy"\))</div>\s*</td>\s*<!-- Transaction Type -->\s*<td>\s*(.*?)\s*</td>\s*<!-- For Archer -->\s*<td>\s*<div class="d-flex align-items-center gap-3">\s*<div class="avatar-cell-initial">(@archerName\[0\].*?)</div>\s*<div class="fw-bold text-white">(@archerName)</div>\s*</div>\s*</td>\s*<!-- Payment -->\s*<td class="text-end fw-bold text-white">\s*(RM @r\.TotalPrice.*?)\s*</td>\s*<!-- Status -->\s*<td class="text-center">\s*<span class="@\(isPaid \? "badge-paid-pill" : "badge-pending-pill"\)">\s*(.*?)\s*</span>\s*</td>\s*<!-- Actions -->\s*<td class="text-end">\s*(.*?)\s*</td>\s*</tr>', re.DOTALL)

res_row_repl = r'''<div class="trans-row" data-status="@statusText" style="background: rgba(255,255,255,0.02); border: 1px solid var(--border-color); border-radius: 14px; padding: 16px 18px; display: flex; align-items: center; justify-content: space-between; gap: 14px; transition: 0.3s; margin-bottom: 14px;">
                                    <!-- Date -->
                                    <div style="flex: 1; min-width: 120px; font-weight: 700; color: #fff; text-align: center; font-size: 0.95rem;">\1</div>
                                    <!-- Transaction Type -->
                                    <div style="flex: 1.5; min-width: 180px; text-align: center;">
                                        \2
                                    </div>
                                    <!-- For Archer -->
                                    <div style="flex: 1.5; min-width: 150px; display: flex; align-items: center; justify-content: center; gap: 12px;">
                                        <div class="avatar-cell-initial" style="width: 32px; height: 32px; font-size: 0.8rem;">\3</div>
                                        <div style="font-weight: 700; color: #fff; font-size: 0.95rem;">\4</div>
                                    </div>
                                    <!-- Payment -->
                                    <div style="flex: 1; min-width: 100px; font-weight: 700; color: #fff; text-align: center; font-size: 0.95rem;">
                                        \5
                                    </div>
                                    <!-- Status -->
                                    <div style="flex: 1; min-width: 100px; display: flex; justify-content: center;">
                                        <span class="@(isPaid ? "badge-paid-pill" : "badge-pending-pill")">
                                            \6
                                        </span>
                                    </div>
                                    <!-- Actions -->
                                    <div style="width: 160px; display: flex; justify-content: center; align-items: center;">
                                        \7
                                    </div>
                                </div>'''

content = res_row_regex.sub(res_row_repl, content)

# Fix empty state
empty_regex = re.compile(r'<tr>\s*<td colspan="6" class="text-center py-5 text-dim">\s*<i class="bi bi-receipt-cutoff d-block mb-3" style="font-size: 2.5rem; opacity: 0.3;"></i>\s*No transaction history found for your account.\s*</td>\s*</tr>')
content = empty_regex.sub(r'<div class="text-center py-5 text-dim" style="background: rgba(255,255,255,0.02); border: 1px solid var(--border-color); border-radius: 14px;"><i class="bi bi-receipt-cutoff d-block mb-3" style="font-size: 2.5rem; opacity: 0.3;"></i>No transaction history found for your account.</div>', content)


with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Done")
