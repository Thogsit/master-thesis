//
// OpenFGA/.NET SDK for OpenFGA
//
// API version: 1.x
// Website: https://openfga.dev
// Documentation: https://openfga.dev/docs
// Support: https://openfga.dev/community
// License: [Apache-2.0](https://github.com/openfga/dotnet-sdk/blob/main/LICENSE)
//
// NOTE: This file was auto generated. DO NOT EDIT.
//


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenFga.Language.Model {
    /// <summary>
    /// Difference
    /// </summary>
    [DataContract(Name = "Difference")]
    public class Difference : IEquatable<Difference>, IValidatableObject {
        /// <summary>
        /// Initializes a new instance of the <see cref="Difference" /> class.
        /// </summary>
        [JsonConstructor]
        public Difference() {
            AdditionalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Difference" /> class.
        /// </summary>
        /// <param name="_base">_base (required).</param>
        /// <param name="subtract">subtract (required).</param>
        public Difference(Userset _base = null, Userset subtract = null) {
            Base = _base ?? throw new ArgumentNullException(nameof(_base));
            Subtract = subtract ?? throw new ArgumentNullException(nameof(subtract));
            AdditionalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or Sets Base
        /// </summary>
        [DataMember(Name = "base", IsRequired = true, EmitDefaultValue = false)]
        [JsonPropertyName("base")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Userset Base { get; set; }

        /// <summary>
        /// Gets or Sets Subtract
        /// </summary>
        [DataMember(Name = "subtract", IsRequired = true, EmitDefaultValue = false)]
        [JsonPropertyName("subtract")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Userset Subtract { get; set; }

        /// <summary>
        /// Gets or Sets additional properties
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; set; }


        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson() {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Builds a Difference from the JSON string presentation of the object
        /// </summary>
        /// <returns>Difference</returns>
        public static Difference FromJson(string jsonString) {
            return JsonSerializer.Deserialize<Difference>(jsonString) ?? throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input) {
            return this.Equals(input as Difference);
        }

        /// <summary>
        /// Returns true if Difference instances are equal
        /// </summary>
        /// <param name="input">Instance of Difference to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Difference input) {
            if (input == null) {
                return false;
            }
            return
                (
                    this.Base == input.Base ||
                    (this.Base != null &&
                    this.Base.Equals(input.Base))
                ) &&
                (
                    this.Subtract == input.Subtract ||
                    (this.Subtract != null &&
                    this.Subtract.Equals(input.Subtract))
                )
                && (this.AdditionalProperties.Count == input.AdditionalProperties.Count && !this.AdditionalProperties.Except(input.AdditionalProperties).Any());
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode() {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 9661;
                if (this.Base != null) {
                    hashCode = (hashCode * 9923) + this.Base.GetHashCode();
                }
                if (this.Subtract != null) {
                    hashCode = (hashCode * 9923) + this.Subtract.GetHashCode();
                }
                if (this.AdditionalProperties != null) {
                    hashCode = (hashCode * 9923) + this.AdditionalProperties.GetHashCode();
                }
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            yield break;
        }

    }

}